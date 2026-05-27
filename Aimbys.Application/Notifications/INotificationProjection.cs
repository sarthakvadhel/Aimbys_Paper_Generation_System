using Aimbys.Domain.Entities;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Maps a domain event to one or more <see cref="Notification"/> rows.
/// Each concrete event type has exactly one registered projection.
/// Projections are invoked by <see cref="IDomainEventDispatcher"/> after
/// <c>SaveChangesAsync</c> commits so they never see uncommitted data.
/// </summary>
public interface INotificationProjection<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Projects the event into zero or more notification rows. Returning
    /// an empty collection means this event does not generate notifications
    /// (e.g. a no-op event for analytics-only).
    /// </summary>
    Task<IReadOnlyList<Notification>> ProjectAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
