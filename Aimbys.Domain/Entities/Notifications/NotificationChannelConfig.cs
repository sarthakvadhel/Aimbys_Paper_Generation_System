namespace Aimbys.Domain.Entities.Notifications;

/// <summary>
/// Platform-level registry of notification channels. The dispatcher
/// fans out only to channels whose <see cref="IsEnabled"/> flag is
/// <c>true</c>; <see cref="ConfigJson"/> holds channel-specific
/// settings (SMTP host for email, gateway URL for SMS, etc.) so a new
/// channel doesn't require a schema migration.
///
/// <para>
/// V1 ships with two seeded rows: <c>"in-app"</c> (always enabled,
/// no config) and <c>"logging"</c> (the catch-all stub from Chunk 10).
/// Production deployments add email / SMS / push by inserting rows;
/// the matching <c>INotificationChannel</c> implementation is
/// registered in DI by the channel package.
/// </para>
/// </summary>
public class NotificationChannelConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Globally unique key (lower-case-kebab-case).</summary>
    public string ChannelKey { get; set; } = string.Empty;

    /// <summary>Human-readable label for the admin UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Channel-specific configuration as JSON. Schema is owned by the
    /// channel implementation; the registry is type-agnostic so adding
    /// a new channel never touches this table's structure.
    /// </summary>
    public string? ConfigJson { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
