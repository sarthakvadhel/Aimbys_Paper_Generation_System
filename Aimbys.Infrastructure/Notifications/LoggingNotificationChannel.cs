using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Catch-all logging channel kept from Chunk 10. Useful in dev /
/// staging as a stand-in for the email/SMS/push channels that ship
/// in later chunks &mdash; emits one Information-level log line per
/// notification so you can verify the dispatcher is reaching the
/// right recipients without provisioning a real delivery backend.
///
/// <para>
/// Production deployments typically disable this channel via the
/// <c>NotificationChannelConfig</c> registry once real channels are
/// wired up; the row stays in DI but the dispatcher skips it when
/// the channel-config lookup returns <c>IsEnabled = false</c>.
/// </para>
/// </summary>
public class LoggingNotificationChannel : INotificationChannel
{
    private readonly ILogger<LoggingNotificationChannel> _logger;

    public LoggingNotificationChannel(ILogger<LoggingNotificationChannel> logger) => _logger = logger;

    public string Key => NotificationChannelKeys.Logging;

    public Task DeliverAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken = default)
    {
        foreach (var n in notifications)
        {
            _logger.LogInformation(
                "[Notification] To={UserId} Severity={Severity} Title=\"{Title}\" Route={RouteUrl}",
                n.RecipientUserId, n.Severity, n.Title, n.RouteUrl);
        }
        return Task.CompletedTask;
    }
}
