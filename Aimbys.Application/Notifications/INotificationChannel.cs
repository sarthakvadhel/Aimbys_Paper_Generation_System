using Aimbys.Domain.Entities;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Delivery channel for notifications. V1 ships
/// <c>InAppNotificationChannel</c> (DB-backed, the existing inbox)
/// and <c>LoggingNotificationChannel</c> (audit/dev stub). Future
/// channels (email, SMS, WhatsApp, push) implement this same
/// interface and are registered in DI; the dispatcher fans out to
/// every registered channel after applying user-preference filtering.
///
/// <para>
/// Implementers expose a stable <see cref="Key"/> matching the
/// <c>ChannelKey</c> used by
/// <see cref="Aimbys.Domain.Entities.Notifications.NotificationPreference"/>
/// and
/// <see cref="Aimbys.Domain.Entities.Notifications.NotificationChannelConfig"/>;
/// <see cref="NotificationChannelKeys"/> defines the canonical
/// values.
/// </para>
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Stable channel discriminator. Lower-case-kebab-case by
    /// convention; matches <c>NotificationPreference.ChannelKey</c>.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Delivers a batch of notifications through this channel. Must
    /// not throw on partial failure; log and continue so one bad
    /// row never blocks the rest of the batch.
    /// </summary>
    Task DeliverAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken = default);
}
