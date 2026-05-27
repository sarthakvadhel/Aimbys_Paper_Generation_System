using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Subscriptions;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Subscriptions;

/// <summary>
/// Implementation of <see cref="ISubscriptionManagementService"/>. Manages
/// institute subscription lifecycle: tier changes, extensions, suspensions,
/// activations, and automated expiration checks.
/// </summary>
public sealed class SubscriptionManagementService : ISubscriptionManagementService
{
    /// <summary>Number of days past expiry before transitioning from GracePeriod to Expired.</summary>
    private const int GracePeriodDays = 7;

    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly ILogger<SubscriptionManagementService> _logger;

    public SubscriptionManagementService(
        AppDbContext db,
        IAuditWriter audit,
        ILogger<SubscriptionManagementService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task ChangeTierAsync(Guid instituteId, LicenseTier newTier, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var institute = await _db.Institutes
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct)
            ?? throw new InvalidOperationException($"Institute {instituteId} not found.");

        var oldTier = institute.LicenseTier;
        institute.LicenseTier = newTier;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Subscription.TierChanged",
            "Institute",
            instituteId.ToString(),
            actor.FindFirstValue(ClaimTypes.NameIdentifier),
            JsonSerializer.Serialize(new { OldTier = oldTier.ToString(), NewTier = newTier.ToString() }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Institute {InstituteId} tier changed from {OldTier} to {NewTier}",
            instituteId, oldTier, newTier);
    }

    public async Task ExtendAsync(Guid instituteId, DateTime newExpiryUtc, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var institute = await _db.Institutes
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct)
            ?? throw new InvalidOperationException($"Institute {instituteId} not found.");

        var oldExpiry = institute.SubscriptionExpiresAtUtc;
        institute.LicenseExpiresAtUtc = newExpiryUtc;
        institute.SubscriptionExpiresAtUtc = newExpiryUtc;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        // If subscription was expired or in grace period, reactivate
        if (institute.SubscriptionStatus is InstituteSubscriptionStatus.Expired or InstituteSubscriptionStatus.GracePeriod)
        {
            institute.SubscriptionStatus = InstituteSubscriptionStatus.Active;
        }

        await _audit.WriteAsync(
            "Subscription.Extended",
            "Institute",
            instituteId.ToString(),
            actor.FindFirstValue(ClaimTypes.NameIdentifier),
            JsonSerializer.Serialize(new { OldExpiry = oldExpiry?.ToString("O"), NewExpiry = newExpiryUtc.ToString("O") }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Institute {InstituteId} subscription extended to {NewExpiry}",
            instituteId, newExpiryUtc);
    }

    public async Task SuspendSubscriptionAsync(Guid instituteId, ClaimsPrincipal actor, string? reason = null, CancellationToken ct = default)
    {
        var institute = await _db.Institutes
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct)
            ?? throw new InvalidOperationException($"Institute {instituteId} not found.");

        var oldStatus = institute.SubscriptionStatus;
        institute.SubscriptionStatus = InstituteSubscriptionStatus.Suspended;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Subscription.Suspended",
            "Institute",
            instituteId.ToString(),
            actor.FindFirstValue(ClaimTypes.NameIdentifier),
            JsonSerializer.Serialize(new { OldStatus = oldStatus.ToString(), Reason = reason }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Institute {InstituteId} subscription suspended. Reason: {Reason}",
            instituteId, reason ?? "(none)");
    }

    public async Task ActivateSubscriptionAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var institute = await _db.Institutes
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct)
            ?? throw new InvalidOperationException($"Institute {instituteId} not found.");

        var oldStatus = institute.SubscriptionStatus;
        institute.SubscriptionStatus = InstituteSubscriptionStatus.Active;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Subscription.Activated",
            "Institute",
            instituteId.ToString(),
            actor.FindFirstValue(ClaimTypes.NameIdentifier),
            JsonSerializer.Serialize(new { OldStatus = oldStatus.ToString() }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Institute {InstituteId} subscription activated (was {OldStatus})",
            instituteId, oldStatus);
    }

    public async Task CheckExpirationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var graceThreshold = now.AddDays(-GracePeriodDays);

        // Find institutes with active subscriptions that have expired
        var expiredInstitutes = await _db.Institutes
            .Where(i => i.SubscriptionStatus == InstituteSubscriptionStatus.Active
                     && i.SubscriptionExpiresAtUtc != null
                     && i.SubscriptionExpiresAtUtc < now)
            .ToListAsync(ct);

        foreach (var institute in expiredInstitutes)
        {
            var expiryDate = institute.SubscriptionExpiresAtUtc!.Value;

            if (expiryDate >= graceThreshold)
            {
                // Within grace window (expired within last 7 days)
                institute.SubscriptionStatus = InstituteSubscriptionStatus.GracePeriod;

                await _audit.WriteAsync(
                    "Subscription.GracePeriodEntered",
                    "Institute",
                    institute.Id.ToString(),
                    null, // system action
                    JsonSerializer.Serialize(new { ExpiresAtUtc = expiryDate.ToString("O"), GracePeriodDays }),
                    cancellationToken: ct);

                _logger.LogInformation("Institute {InstituteId} entered grace period (expired {ExpiryDate})",
                    institute.Id, expiryDate);
            }
            else
            {
                // Past grace window
                institute.SubscriptionStatus = InstituteSubscriptionStatus.Expired;

                await _audit.WriteAsync(
                    "Subscription.Expired",
                    "Institute",
                    institute.Id.ToString(),
                    null,
                    JsonSerializer.Serialize(new { ExpiresAtUtc = expiryDate.ToString("O"), GracePeriodDays }),
                    cancellationToken: ct);

                _logger.LogInformation("Institute {InstituteId} subscription expired (past grace period)",
                    institute.Id);
            }

            institute.UpdatedAtUtc = now;
        }

        // Also check institutes currently in grace period that have now exceeded the window
        var gracePeriodExpired = await _db.Institutes
            .Where(i => i.SubscriptionStatus == InstituteSubscriptionStatus.GracePeriod
                     && i.SubscriptionExpiresAtUtc != null
                     && i.SubscriptionExpiresAtUtc < graceThreshold)
            .ToListAsync(ct);

        foreach (var institute in gracePeriodExpired)
        {
            institute.SubscriptionStatus = InstituteSubscriptionStatus.Expired;
            institute.UpdatedAtUtc = now;

            await _audit.WriteAsync(
                "Subscription.Expired",
                "Institute",
                institute.Id.ToString(),
                null,
                JsonSerializer.Serialize(new { ExpiresAtUtc = institute.SubscriptionExpiresAtUtc!.Value.ToString("O"), GracePeriodDays }),
                cancellationToken: ct);

            _logger.LogInformation("Institute {InstituteId} grace period elapsed — marked Expired",
                institute.Id);
        }

        if (expiredInstitutes.Count > 0 || gracePeriodExpired.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }
}
