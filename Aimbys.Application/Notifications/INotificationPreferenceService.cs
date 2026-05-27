using Aimbys.Domain.Entities.Notifications;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Read/write surface for per-user channel preferences. Consumed by:
///
/// <list type="bullet">
///   <item>The dispatcher, which calls
///         <see cref="ShouldDeliverAsync"/> before fanning out a
///         notification to a given channel + recipient.</item>
///   <item>The (future) preferences UI, which uses
///         <see cref="GetPreferencesAsync"/> /
///         <see cref="SetPreferenceAsync"/>.</item>
/// </list>
///
/// <para>
/// Defaults: a user with no preference row for a channel is treated
/// as <em>enabled at <see cref="NotificationSeverity.Information"/></em>
/// so opt-in is automatic and only opt-outs need explicit storage.
/// </para>
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Returns every preference row stored for the user. The list may
    /// be empty &mdash; that's a valid "all defaults" state.
    /// </summary>
    Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts the preference for (<paramref name="userId"/>,
    /// <paramref name="channelKey"/>). Same row on re-call &mdash;
    /// uniqueness is enforced at the DB level.
    /// </summary>
    Task SetPreferenceAsync(
        string userId,
        string channelKey,
        bool isEnabled,
        NotificationSeverity minimumSeverity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decision gate the dispatcher calls before invoking a channel.
    /// Returns <c>true</c> when:
    /// <list type="bullet">
    ///   <item>The user has no opt-out (defaults treat the channel as enabled), OR</item>
    ///   <item>The opt-out exists but the notification's severity meets / exceeds the configured minimum.</item>
    /// </list>
    /// </summary>
    Task<bool> ShouldDeliverAsync(
        string userId,
        string channelKey,
        NotificationSeverity severity,
        CancellationToken cancellationToken = default);
}
