using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AsistanApp.Services;

public sealed class AuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> users,
        AppDbContext db,
        IConfiguration configuration)
    {
        _users = users;
        _db = db;
        _configuration = configuration;
    }

    public async Task<(ApplicationUser User, AuthTokens Tokens)?> LoginAsync(
        string email,
        string password,
        string ip,
        string? requiredAccountType = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByEmailAsync(email.Trim());
        if (user is null
            || await _users.IsLockedOutAsync(user)
            || !await _users.CheckPasswordAsync(user, password)
            || requiredAccountType is not null
                && !string.Equals(user.AccountType, requiredAccountType, StringComparison.Ordinal))
            return null;

        user.LastAuthenticatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        var tokens = await CreateSessionAsync(user, ip, cancellationToken);
        return (user, tokens);
    }

    public async Task<(ApplicationUser User, AuthTokens Tokens)> RegisterAsync(
        string email,
        string password,
        string displayName,
        string accountType,
        string? elderlyOwnerId,
        string ip,
        CancellationToken cancellationToken = default)
    {
        if (accountType is not ("Elderly" or "Family"))
            throw new InvalidOperationException("Invalid account type.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = email.Trim(),
            Email = email.Trim(),
            DisplayName = displayName.Trim(),
            AccountType = accountType,
            ElderlyOwnerId = elderlyOwnerId,
            EmailConfirmed = false,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow,
            LastAuthenticatedAt = DateTime.UtcNow
        };
        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new IdentityValidationException(result.Errors.Select(e => e.Description));

        await _users.AddToRoleAsync(user, accountType);
        var tokens = await CreateSessionAsync(user, ip, cancellationToken);
        return (user, tokens);
    }

    public async Task<(ApplicationUser User, AuthTokens Tokens)?> RefreshAsync(
        string refreshToken,
        string ip,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var session = await _db.RefreshSessions
            .SingleOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTime.UtcNow)
            return null;

        var user = await _users.FindByIdAsync(session.UserId);
        if (user is null || await _users.IsLockedOutAsync(user))
            return null;

        session.RevokedAt = DateTime.UtcNow;
        var replacement = await CreateSessionAsync(user, ip, cancellationToken, save: false);
        session.ReplacedByTokenHash = HashToken(replacement.RefreshToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (user, replacement);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var session = await _db.RefreshSessions
            .SingleOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
        if (session is null || session.RevokedAt is not null) return;
        session.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _db.RefreshSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.RevokedAt, DateTime.UtcNow),
                cancellationToken);
    }

    private async Task<AuthTokens> CreateSessionAsync(
        ApplicationUser user,
        string ip,
        CancellationToken cancellationToken,
        bool save = true)
    {
        var now = DateTime.UtcNow;
        var accessMinutes = _configuration.GetValue("Jwt:AccessTokenMinutes", 15);
        var refreshDays = _configuration.GetValue("Jwt:RefreshTokenDays", 30);
        var accessExpires = now.AddMinutes(accessMinutes);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.AccountType),
            new("account_type", user.AccountType),
            new("elderly_id", user.AccountType == "Elderly" ? user.Id : user.ElderlyOwnerId ?? ""),
            new("auth_time", new DateTimeOffset(user.LastAuthenticatedAt ?? now).ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is missing.")));
        var jwt = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        _db.RefreshSessions.Add(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshDays),
            CreatedByIp = ip
        });
        if (save)
            await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            refreshToken,
            accessExpires);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed class IdentityValidationException : Exception
{
    public IReadOnlyCollection<string> Errors { get; }

    public IdentityValidationException(IEnumerable<string> errors)
        : base("Identity validation failed.")
    {
        Errors = errors.ToArray();
    }
}