namespace Aimbys.Application.Notifications;

/// <summary>
/// Canonical channel keys recognised by the notification pipeline.
/// Held as <c>const</c> strings so projection / preference / config
/// code never spells a key by hand &mdash; a typo is a compile-time
/// error rather than a silently-skipped delivery.
/// </summary>
public static class NotificationChannelKeys
{
    /// <summary>Persistent in-app inbox (the bell icon + activity feed).</summary>
    public const string InApp = "in-app";

    /// <summary>Catch-all logging stub from Chunk 10 (no real delivery).</summary>
    public const string Logging = "logging";

    /// <summary>Email delivery (SMTP-backed; channel implementation lands later).</summary>
    public const string Email = "email";

    /// <summary>SMS delivery (gateway-backed; channel implementation lands later).</summary>
    public const string Sms = "sms";

    /// <summary>Mobile-push delivery (FCM/APNs-backed; lands later).</summary>
    public const string Push = "push";
}
