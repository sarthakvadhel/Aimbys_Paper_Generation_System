using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Single sanctioned route for turning a template + placeholder
/// dictionary into a ready-to-persist <see cref="Notification"/>.
/// Projections call into this surface instead of building strings by
/// hand so:
///
/// <list type="bullet">
///   <item>Copy is editable in the database without redeploys.</item>
///   <item>Multilingual rendering is a single property change at the
///         call site.</item>
///   <item>Default severity / route patterns are owned by the
///         template, not the projection.</item>
/// </list>
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Renders the template identified by <paramref name="templateKey"/>
    /// for the given recipient.
    ///
    /// <para>
    /// Language fallback chain:
    /// </para>
    /// <list type="number">
    ///   <item>If <paramref name="languageCode"/> matches a
    ///         translation, use it.</item>
    ///   <item>Otherwise use <c>en-IN</c> if a translation exists.</item>
    ///   <item>Otherwise use the template's default (the columns on
    ///         the parent <c>NotificationTemplate</c> row).</item>
    /// </list>
    ///
    /// <para>
    /// Unknown template keys throw
    /// <see cref="System.Collections.Generic.KeyNotFoundException"/>;
    /// inactive templates return <c>null</c> so projections can opt
    /// out of delivery without an exception.
    /// </para>
    /// </summary>
    Task<Notification?> RenderAsync(
        string templateKey,
        string recipientUserId,
        IReadOnlyDictionary<string, string?> placeholders,
        Guid? instituteId = null,
        string? languageCode = null,
        NotificationSeverity? severityOverride = null,
        CancellationToken cancellationToken = default);
}
