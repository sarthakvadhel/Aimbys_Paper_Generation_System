using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Notifications;

/// <summary>
/// Reusable notification template. Drives the title and body of a
/// <see cref="Notification"/> row instead of hard-coding strings inside
/// projections, so messaging copy can be edited / localized / A/B-tested
/// without touching projection code.
///
/// <para>
/// Templates use simple <c>{placeholder}</c> interpolation: the renderer
/// substitutes named placeholders from a supplied dictionary and leaves
/// any unmatched placeholders intact (with a logger warning) rather
/// than throwing &mdash; a missing value is better than a missing
/// notification.
/// </para>
///
/// <para>
/// <see cref="Key"/> is globally unique (e.g.
/// <c>"PaperApproved"</c>, <c>"EvaluationAssigned"</c>); projections
/// reference templates by key, never by primary key, so seeding can
/// recreate the same template with a fresh row id without breaking the
/// link.
/// </para>
/// </summary>
public class NotificationTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable, dot- or PascalCase key. Globally unique.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Default-language (en-IN) title with <c>{placeholder}</c> markers,
    /// e.g. <c>"Paper \"{paperTitle}\" approved"</c>.
    /// </summary>
    public string TitleTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Default-language body. Same interpolation grammar as
    /// <see cref="TitleTemplate"/>; nullable so notifications without
    /// a body (toast-style) are representable.
    /// </summary>
    public string? BodyTemplate { get; set; }

    /// <summary>
    /// Default severity used when the projection doesn't specify one
    /// explicitly. Per-render overrides are still allowed.
    /// </summary>
    public NotificationSeverity DefaultSeverity { get; set; } = NotificationSeverity.Information;

    /// <summary>
    /// Optional route pattern with placeholders, e.g.
    /// <c>"/Institute/Papers/Details/{paperId}"</c>. Rendered through
    /// the same interpolator as the title/body.
    /// </summary>
    public string? DefaultRoutePattern { get; set; }

    /// <summary>Free-form description shown to admins managing templates.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Disables the template without deleting it &mdash; useful when a
    /// projection is being retired and admins want to confirm zero
    /// recipients before purging.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<NotificationTemplateTranslation> Translations { get; set; }
        = new List<NotificationTemplateTranslation>();
}
