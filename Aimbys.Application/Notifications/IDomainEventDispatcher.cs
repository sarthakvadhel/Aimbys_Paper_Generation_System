using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Dispatches domain events raised during a unit-of-work after the
/// <c>SaveChangesAsync</c> call commits. The dispatcher resolves the
/// matching <see cref="INotificationProjection{TEvent}"/> for each event,
/// projects notifications, persists them, and forwards to
/// <see cref="INotificationChannel"/>s.
///
/// The implementation lives in Infrastructure (via
/// <c>ISaveChangesInterceptor</c>) so events are released only on
/// persistence success.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a batch of events. Called internally by the interceptor
    /// after commit; services may also call it directly when they compose
    /// events outside EF (rare but allowed).
    /// </summary>
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);
}
