using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Default in-app delivery channel: persists notifications to the
/// <c>Notifications</c> table via <see cref="INotificationService"/>
/// so the bell badge / activity feed picks them up. The dispatcher
/// applies user-preference filtering before invoking
/// <see cref="DeliverAsync"/>, so a user who disabled the in-app
/// channel sees nothing in their inbox &mdash; the row is dropped at
/// the channel boundary, not stored-and-hidden.
///
/// <para>
/// This channel replaces the unconditional persist step that lived
/// inside <c>DomainEventDispatcher</c> in Chunk 10. Direct callers
/// of <see cref="INotificationService.CreateBatchAsync"/> (e.g.
/// <c>WorkflowEscalationService</c>) still bypass the channel
/// pipeline by design &mdash; system-level escalations are not
/// subject to user preferences.
/// </para>
/// </summary>
public sealed class InAppNotificationChannel : INotificationChannel
{
    private readonly INotificationService _notifications;
    private readonly ILogger<InAppNotificationChannel> _logger;

    public InAppNotificationChannel(
        INotificationService notifications,
        ILogger<InAppNotificationChannel> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public string Key => NotificationChannelKeys.InApp;

    public async Task DeliverAsync(
        IReadOnlyList<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0) return;

        try
        {
            await _notifications.CreateBatchAsync(notifications, cancellationToken);
        }
        catch (Exception ex)
        {
            // Channel contract: never throw on partial failure; log
            // and let the rest of the dispatch fan-out continue.
            _logger.LogError(ex,
                "InAppNotificationChannel failed to persist {Count} notification(s).",
                notifications.Count);
        }
    }
}
