using System.Security.Claims;

namespace Aimbys.Application.Subscriptions;

/// <summary>
/// Service interface for managing institute subscriptions and license tiers.
/// SuperAdmin-facing operations: change tier, extend, suspend, activate.
/// System-facing: check expirations (called by scheduled job).
/// </summary>
public interface ISubscriptionManagementService
{
    Task ChangeTierAsync(Guid instituteId, Aimbys.Domain.Enums.LicenseTier newTier, ClaimsPrincipal actor, CancellationToken ct = default);
    Task ExtendAsync(Guid instituteId, DateTime newExpiryUtc, ClaimsPrincipal actor, CancellationToken ct = default);
    Task SuspendSubscriptionAsync(Guid instituteId, ClaimsPrincipal actor, string? reason = null, CancellationToken ct = default);
    Task ActivateSubscriptionAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task CheckExpirationsAsync(CancellationToken ct = default);
}
