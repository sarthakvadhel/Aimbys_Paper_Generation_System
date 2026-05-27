using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Persistent in-app notification row. Institute-scoped so queries can
/// filter by tenant; <see cref="RecipientUserId"/> is the delivery target.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tenancy boundary. Null for platform-level (SuperAdmin) notifications.</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>FK to <c>AspNetUsers.Id</c>.</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>Short headline, max 200 chars.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer body, max 2000 chars.</summary>
    public string? Body { get; set; }

    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Information;

    /// <summary>
    /// Optional relative route the user should navigate to when clicking
    /// the notification (e.g. <c>/Teacher/Evaluation/abc</c>). Null means
    /// the notification is informational only.
    /// </summary>
    public string? RouteUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
