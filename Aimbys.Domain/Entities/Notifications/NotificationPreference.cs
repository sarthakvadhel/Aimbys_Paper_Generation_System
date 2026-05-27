using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Notifications;

/// <summary>
/// Per-user delivery preference for a single notification channel. A
/// user can mute Information-level in-app notifications while still
/// receiving Warning+ via email, for example.
///
/// <para>
/// Composite uniqueness: (UserId, ChannelKey). Reads happen on every
/// notification dispatch, so the row is intentionally tiny.
/// </para>
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <c>AspNetUsers.Id</c>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Channel discriminator (e.g. <c>"in-app"</c>, <c>"email"</c>,
    /// <c>"sms"</c>). Lower-case-kebab-case by convention; matches
    /// <see cref="NotificationChannelConfig.ChannelKey"/>.
    /// </summary>
    public string ChannelKey { get; set; } = string.Empty;

    /// <summary>True when the user has the channel enabled. Default: true.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Minimum severity that should be delivered through this channel.
    /// Notifications with a lower severity are dropped at the
    /// channel-fanout step. Default <see cref="NotificationSeverity.Information"/>
    /// (deliver everything).
    /// </summary>
    public NotificationSeverity MinimumSeverity { get; set; } = NotificationSeverity.Information;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
