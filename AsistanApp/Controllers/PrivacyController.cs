using System.Security.Claims;
using AsistanApp.Services;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ilk_projem.Controllers;

[ApiController]
[Authorize]
[Route("api/privacy")]
public sealed class PrivacyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuthService _auth;

    public PrivacyController(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        AuthService auth)
    {
        _db = db;
        _users = users;
        _auth = auth;
    }

    [HttpPost("consent")]
    public async Task<IResult> RecordConsent(
        [FromBody] ConsentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyVersion))
            return Results.BadRequest(new { success = false, message = "Policy version is required." });
        _db.PrivacyConsents.Add(new PrivacyConsent
        {
            UserId = UserId(),
            PolicyVersion = request.PolicyVersion.Trim(),
            Accepted = request.Accepted,
            RecordedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { success = true });
    }

    [HttpGet("export")]
    public async Task<IResult> Export(CancellationToken cancellationToken)
    {
        var userId = UserId();
        var elderlyId = ElderlyId();
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                u.Email,
                u.PhoneNumber,
                u.AccountType,
                u.ElderlyOwnerId,
                u.CreatedAt
            })
            .SingleAsync(cancellationToken);

        object ownedData = user.AccountType == "Elderly"
            ? new
            {
                healthRecords = await _db.HealthRecords.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                medications = await _db.Medications.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                moods = await _db.MoodRecords.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                familyMembers = await _db.FamilyMembers.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                emergencyAlerts = await _db.EmergencyAlerts.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                tasks = await _db.TaskItems.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken),
                notifications = await _db.Notifications.AsNoTracking().Where(x => x.ElderlyId == elderlyId).ToListAsync(cancellationToken)
            }
            : new { };

        var consents = await _db.PrivacyConsents.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        return Results.Ok(new
        {
            exportedAt = DateTime.UtcNow,
            user,
            consents,
            data = ownedData
        });
    }

    [HttpGet("deletion-status")]
    public async Task<IResult> DeletionStatus(CancellationToken cancellationToken)
    {
        var requestedAt = await _db.Users.AsNoTracking()
            .Where(u => u.Id == UserId())
            .Select(u => u.DeletionRequestedAt)
            .SingleOrDefaultAsync(cancellationToken);
        return Results.Ok(new { success = true, status = "active", requestedAt });
    }

    [HttpDelete("/api/elderly/account")]
    [HttpDelete("/api/family/account")]
    public async Task<IResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(UserId());
        if (user is null) return Results.Unauthorized();
        if (!await _users.CheckPasswordAsync(user, request.Password))
            return Results.Json(new { success = false, message = "Şifre doğrulanamadı." }, statusCode: 401);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (user.AccountType == "Elderly")
        {
            var familyAccounts = await _db.Users
                .Where(u => u.ElderlyOwnerId == user.Id)
                .ToListAsync(cancellationToken);
            foreach (var family in familyAccounts)
            {
                var familyDeleted = await _users.DeleteAsync(family);
                if (!familyDeleted.Succeeded)
                    throw new InvalidOperationException("Linked family account deletion failed.");
            }

            var events = await _db.RevenueCatEvents
                .Where(e => e.AppUserId == user.Id)
                .ToListAsync(cancellationToken);
            foreach (var revenueEvent in events)
                revenueEvent.AppUserId = $"deleted-{Guid.NewGuid():N}";
        }

        await _auth.RevokeAllAsync(user.Id, cancellationToken);
        var deleted = await _users.DeleteAsync(user);
        if (!deleted.Succeeded)
            return Results.BadRequest(new { success = false, errors = deleted.Errors.Select(e => e.Description) });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(new { success = true, message = "Hesap ve ilişkili kişisel veriler silindi." });
    }

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private string ElderlyId() =>
        User.FindFirstValue("elderly_id")
        ?? throw new UnauthorizedAccessException();
}

public sealed record ConsentRequest(string PolicyVersion, bool Accepted);
public sealed record DeleteAccountRequest(string Password);
