using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Dispatcher: for each event, resolves its
/// <see cref="INotificationProjection{TEvent}"/> from DI, projects
/// notifications, then fans out to every registered
/// <see cref="INotificationChannel"/> after applying per-user
/// preference filtering.
///
/// <para>
/// In Chunk 13 the explicit "always persist" call to
/// <see cref="INotificationService.CreateBatchAsync"/> moves into
/// <see cref="InAppNotificationChannel"/>, so a user who opts out of
/// the in-app channel actually doesn't see rows in their inbox &mdash;
/// the row is dropped at the channel boundary, not stored-and-hidden.
/// Direct callers of <c>INotificationService.CreateBatchAsync</c>
/// (e.g. <c>WorkflowEscalationService</c>) bypass the channel
/// pipeline by design: system-level escalations are not subject to
/// user preferences.
/// </para>
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly INotificationPreferenceService _preferences;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IServiceProvider services,
        IEnumerable<INotificationChannel> channels,
        INotificationPreferenceService preferences,
        ILogger<DomainEventDispatcher> logger)
    {
        _services = services;
        _channels = channels;
        _preferences = preferences;
        _logger = logger;
    }

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events is null || events.Count == 0) return;

        var allNotifications = new List<Notification>();

        foreach (var domainEvent in events)
        {
            try
            {
                var notifications = await ProjectEventAsync(domainEvent, cancellationToken);
                if (notifications.Count > 0)
                {
                    allNotifications.AddRange(notifications);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to project domain event {EventType}. Skipping.",
                    domainEvent.GetType().Name);
            }
        }

        if (allNotifications.Count == 0) return;

        // Per-channel preference filter, then deliver. The InApp channel
        // performs the DB persistence so opt-outs actually mean "no row
        // in the inbox" rather than "stored but hidden from view".
        foreach (var channel in _channels)
        {
            var allowed = new List<Notification>(allNotifications.Count);

            foreach (var n in allNotifications)
            {
                bool shouldDeliver;
                try
                {
                    shouldDeliver = await _preferences.ShouldDeliverAsync(
                        n.RecipientUserId, channel.Key, n.Severity, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Preference lookup must never block delivery; on
                    // failure we default to delivering so a degraded
                    // preference store doesn't silently drop messages.
                    _logger.LogWarning(ex,
                        "Preference lookup failed for user {UserId} channel {Channel}; defaulting to deliver.",
                        n.RecipientUserId, channel.Key);
                    shouldDeliver = true;
                }

                if (shouldDeliver)
                {
                    allowed.Add(n);
                }
            }

            if (allowed.Count == 0) continue;

            try
            {
                await channel.DeliverAsync(allowed, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Notification channel {Channel} failed; remaining channels continue.",
                    channel.Key);
            }
        }
    }

    private Task<IReadOnlyList<Notification>> ProjectEventAsync(
        IDomainEvent domainEvent, CancellationToken ct)
    {
        // Resolve INotificationProjection<TConcreteEvent> from DI via
        // reflection on the runtime event type.
        var eventType = domainEvent.GetType();
        var projectionType = typeof(INotificationProjection<>).MakeGenericType(eventType);
        var projection = _services.GetService(projectionType);

        if (projection is null)
        {
            _logger.LogDebug(
                "No projection registered for {EventType}. No notifications emitted.",
                eventType.Name);
            return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
        }

        // Call ProjectAsync via reflection (the generic type is only known at
        // runtime). The method signature is:
        //   Task<IReadOnlyList<Notification>> ProjectAsync(TEvent, CancellationToken)
        var method = projectionType.GetMethod(nameof(INotificationProjection<IDomainEvent>.ProjectAsync))!;
        var task = (Task<IReadOnlyList<Notification>>)method.Invoke(projection, new object[] { domainEvent, ct })!;
        return task;
    }
}
