namespace Aimbys.Domain.Enums;

/// <summary>
/// Subscription / billing lifecycle for an
/// <see cref="Aimbys.Domain.Entities.Institute"/>. Distinct from
/// <see cref="InstituteStatus"/> which tracks SuperAdmin governance:
/// an institute can be governance-Active but billing-Expired (read-only
/// access until renewal).
///
/// <para>
/// Consumed by
/// <c>SubscriptionEnforcementMiddleware</c> to gate request handling
/// for the institute's user surface (<c>/Institute/*</c>,
/// <c>/Teacher/*</c>, <c>/Student/*</c>).
/// </para>
/// </summary>
public enum InstituteSubscriptionStatus
{
    /// <summary>Default state on onboarding; full feature access during the trial window.</summary>
    Trial = 0,

    /// <summary>Paid subscription in good standing.</summary>
    Active = 1,

    /// <summary>Manually paused by SuperAdmin (e.g. unpaid invoice). Read-only access.</summary>
    Suspended = 2,

    /// <summary>Subscription has elapsed past its grace period; full block.</summary>
    Expired = 3,

    /// <summary>Past the expiry date but inside the configured grace window. Read-only.</summary>
    GracePeriod = 4,

    /// <summary>Renewal is in progress (invoice issued, payment pending). Full access continues.</summary>
    RenewalPending = 5
}
