using System.Security.Claims;
using AsistanApp.Services;
using ilk_projem.Models.Persistence;
using ilk_projem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ilk_projem.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("elderly/login")]
    public async Task<IResult> ElderlyLogin(
        [FromBody] LoginRequest request,
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { success = false, message = "E-posta ve şifre zorunludur" });

        var result = await auth.LoginAsync(
            request.Email,
            request.Password,
            ClientIp(),
            "Elderly",
            cancellationToken);
        return result is null
            ? Results.Json(new { success = false, message = "Geçersiz kimlik bilgileri" }, statusCode: 401)
            : AuthResponse(result.Value.User, result.Value.Tokens);
    }

    [AllowAnonymous]
    [HttpPost("elderly-self-enroll")]
    public async Task<IResult> RegisterElderly(
        [FromBody] ElderlyRegistrationRequest request,
        [FromServices] AuthService auth,
        [FromServices] UserManager<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await auth.RegisterAsync(
                request.Email,
                request.Password,
                request.FullName,
                "Elderly",
                null,
                ClientIp(),
                cancellationToken);
            result.User.PhoneNumber = request.Phone?.Trim();
            result.User.BirthDate = request.BirthDate?.Trim() ?? "";
            await users.UpdateAsync(result.User);
            return AuthResponse(result.User, result.Tokens);
        }
        catch (IdentityValidationException ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Errors.FirstOrDefault(), errors = ex.Errors });
        }
    }

    [AllowAnonymous]
    [HttpPost("auth/refresh")]
    public async Task<IResult> Refresh(
        [FromBody] RefreshRequest request,
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        var result = await auth.RefreshAsync(request.RefreshToken, ClientIp(), cancellationToken);
        return result is null
            ? Results.Json(new { success = false, message = "Oturum yenilenemedi" }, statusCode: 401)
            : AuthResponse(result.Value.User, result.Value.Tokens);
    }

    [AllowAnonymous]
    [HttpPost("elderly/reset-password")]
    public async Task<IResult> RequestPasswordReset(
        [FromBody] PasswordResetRequest request,
        [FromServices] UserManager<ApplicationUser> users,
        [FromServices] AccountEmailService emailService,
        [FromServices] ILogger<AuthController> logger)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is not null)
        {
            try
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                await emailService.SendPasswordResetAsync(user.Email!, token, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Password reset email could not be sent.");
            }
        }
        return Results.Ok(new
        {
            success = true,
            message = "Hesap varsa şifre yenileme bağlantısı e-posta adresine gönderildi."
        });
    }

    [AllowAnonymous]
    [HttpPost("elderly/reset-password/confirm")]
    public async Task<IResult> ConfirmPasswordReset(
        [FromBody] PasswordResetConfirmRequest request,
        [FromServices] UserManager<ApplicationUser> users,
        [FromServices] AuthService auth)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Results.BadRequest(new { success = false, message = "Geçersiz veya süresi dolmuş bağlantı." });

        var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return Results.BadRequest(new
            {
                success = false,
                message = "Geçersiz veya süresi dolmuş bağlantı.",
                errors = result.Errors.Select(e => e.Description)
            });

        await auth.RevokeAllAsync(user.Id, HttpContext.RequestAborted);
        return Results.Ok(new { success = true, message = "Şifre güncellendi." });
    }

    [Authorize]
    [HttpPost("auth/logout")]
    public async Task<IResult> Logout(
        [FromBody] RefreshRequest request,
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        await auth.RevokeAsync(request.RefreshToken, cancellationToken);
        return Results.Ok(new { success = true });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IResult> Me([FromServices] UserManager<ApplicationUser> users)
    {
        var user = await users.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? "");
        return user is null
            ? Results.Unauthorized()
            : Results.Json(new
            {
                success = true,
                user = new
                {
                    user.Id,
                    name = user.DisplayName,
                    user.Email,
                    user.AccountType,
                    elderlyId = user.AccountType == "Elderly" ? user.Id : user.ElderlyOwnerId
                }
            });
    }

    private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static IResult AuthResponse(ApplicationUser user, AuthTokens tokens) =>
        Results.Json(new
        {
            success = true,
            token = tokens.AccessToken,
            refreshToken = tokens.RefreshToken,
            expiresAt = tokens.ExpiresAt,
            userId = user.Id,
            name = user.DisplayName,
            accountType = user.AccountType,
            caringFor = user.AccountType == "Family" ? user.ElderlyOwnerId : null
        });
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record PasswordResetRequest(string Email);
public sealed record PasswordResetConfirmRequest(string Email, string Token, string NewPassword);
public sealed record ElderlyRegistrationRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? BirthDate);
