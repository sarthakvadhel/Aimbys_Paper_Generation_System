using System.Security.Claims;
using Aimbys.Domain.Entities.Broadcasts;

namespace Aimbys.Application.Broadcasts;

/// <summary>
/// Single sanctioned route for managing platform-wide broadcasts.
/// SuperAdmin schedules a broadcast; the layout's broadcast banner
/// partial calls <see cref="GetActiveAsync"/> to fetch what to render
/// for the current role area.
///
/// <para>
/// Implementations are expected to sanitize <c>BodyHtml</c> at create
/// time (broadcasts render with <c>@@Html.Raw</c> in the layout) and
/// schedule a deferred deactivation job for <c>EndsAtUtc</c> so the
/// banner disappears at the right moment without a refresh.
/// </para>
/// </summary>
public interface IBroadcastService
{
    /// <summary>
    /// Persists a new broadcast and returns its id. Sanitizes
    /// <see cref="BroadcastCreateRequest.BodyHtml"/> before saving.
    /// </summary>
    Task<Guid> CreateAsync(
        BroadcastCreateRequest request,
        ClaimsPrincipal actor,
        CancellationToken ct = default);

    /// <summary>
    /// Returns every non-deleted broadcast ordered by <c>StartsAtUtc</c>
    /// descending. Used by the SuperAdmin governance screen.
    /// </summary>
    Task<IReadOnlyList<Broadcast>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns broadcasts that are currently active (within their
    /// active window) and target the given role area. Result is cached
    /// per role area to keep the layout cheap.
    /// </summary>
    Task<IReadOnlyList<Broadcast>> GetActiveAsync(
        string roleArea,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a broadcast (sets <c>IsActive = false</c>). Returns
    /// <c>false</c> when the broadcast id is unknown.
    /// </summary>
    Task<bool> CancelAsync(Guid id, ClaimsPrincipal actor, CancellationToken ct = default);
}

/// <summary>
/// Inputs to <see cref="IBroadcastService.CreateAsync"/>. Audience
/// filter is a JSON document so the schema can evolve without
/// breaking callers; today the only key consulted is <c>roles</c>.
/// </summary>
public sealed record BroadcastCreateRequest(
    string Subject,
    string BodyHtml,
    string AudienceFilterJson,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc);
