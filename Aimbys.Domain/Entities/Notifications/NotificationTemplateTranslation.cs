namespace Aimbys.Domain.Entities.Notifications;

/// <summary>
/// Per-language variant of a <see cref="NotificationTemplate"/>. The
/// renderer walks the translation chain
/// (<c>requested → en-IN base → template default</c>) so a missing
/// translation never blocks a notification.
///
/// <para>
/// (TemplateId, LanguageCode) is unique. <see cref="LanguageCode"/>
/// follows BCP-47 (<c>en-IN</c>, <c>hi-IN</c>, …) so the existing
/// browser <c>Accept-Language</c> header can be matched without
/// translation tables.
/// </para>
/// </summary>
public class NotificationTemplateTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TemplateId { get; set; }

    /// <summary>BCP-47 language tag, e.g. <c>"hi-IN"</c>.</summary>
    public string LanguageCode { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string? BodyTemplate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public NotificationTemplate? Template { get; set; }
}
