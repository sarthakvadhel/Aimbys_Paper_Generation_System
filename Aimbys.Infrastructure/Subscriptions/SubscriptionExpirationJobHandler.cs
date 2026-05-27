using Aimbys.Application.Scheduling;
using Aimbys.Application.Subscriptions;

namespace Aimbys.Infrastructure.Subscriptions;

/// <summary>
/// Scheduled job handler that checks subscription expirations daily.
/// Registered in DI as <see cref="IScheduledJobHandler"/>; the scheduling
/// host dispatches it when a <c>ScheduledJob</c> row with
/// <c>JobKey == "subscription.checkExpirations"</c> comes due.
///
/// The recurring job row should be seeded via the SuperAdmin scheduling
/// page or via data seed with cron expression <c>"0 2 * * *"</c> (daily 2 AM UTC).
/// </summary>
public sealed class SubscriptionExpirationJobHandler : IScheduledJobHandler
{
    public string JobKey => "subscription.checkExpirations";

    private readonly ISubscriptionManagementService _subscriptions;

    public SubscriptionExpirationJobHandler(ISubscriptionManagementService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        return _subscriptions.CheckExpirationsAsync(cancellationToken);
    }
}
