using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Broadcasts;
using Aimbys.Application.Scheduling;
using Aimbys.Domain.Entities.Broadcasts;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Infrastructure.Questions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Broadcasts;

/// <summary>
/// Default <see cref="IBroadcastService"/>. Persists broadcasts via
/// <see cref="AppDbContext"/>, sanitizes the body HTML at create time
/// (the layout banner renders with <c>@@Html.Raw</c>), schedules a
/// best-effort deactivation job for <c>EndsAtUtc</c>, and caches the
/// active-broadcasts result per role area for 60 seconds.
///
/// <para>
/// Audit rows are written via <see cref="IAuditWriter"/> and committed
/// alongside the entity change in a single SaveChanges so the trail
/// stays consistent with the persisted state.
/// </para>
/// </summary>
public sealed class BroadcastService : IBroadcastService
{
    /// <summary>Cache window for per-role active broadcasts.</summary>
    public static readonly TimeSpan ActiveCacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>Job key used when scheduling a deactivation tick.</summary>
    public const string DeactivateJobKey = "broadcast.deactivate";

    private const string ActiveCacheKeyPrefix = "broadcasts:active:";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ISchedulingService _scheduling;
    private readonly IAuditWriter _audit;
    private readonly ILogger<BroadcastService> _logger;

    public BroadcastService(
        AppDbContext db,
        IMemoryCache cache,
        ISchedulingService scheduling,
        IAuditWriter audit,
        ILogger<BroadcastService> logger)
    {
        _db = db;
        _cache = cache;
        _scheduling = scheduling;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        BroadcastCreateRequest request,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("Subject is required.", nameof(request));
        if (request.EndsAtUtc <= request.StartsAtUtc)
            throw new ArgumentException("EndsAtUtc must be greater than StartsAtUtc.", nameof(request));

        var actorId = actor.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;

        var broadcast = new Broadcast
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject.Trim(),
            BodyHtml = HtmlSanitizer.Sanitize(request.BodyHtml),
            AudienceFilterJson = string.IsNullOrWhiteSpace(request.AudienceFilterJson)
                ? "{}"
                : request.AudienceFilterJson,
            StartsAtUtc = DateTime.SpecifyKind(request.StartsAtUtc, DateTimeKind.Utc),
            EndsAtUtc = DateTime.SpecifyKind(request.EndsAtUtc, DateTimeKind.Utc),
            CreatedByUserId = actorId,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _db.Broadcasts.Add(broadcast);

        await _audit.WriteAsync(
            action: "Broadcast.Created",
            entityType: nameof(Broadcast),
            entityId: broadcast.Id.ToString(),
            actorUserId: string.IsNullOrEmpty(actorId) ? null : actorId,
            detailsJson: JsonSerializer.Serialize(new
            {
                broadcast.Subject,
                broadcast.StartsAtUtc,
                broadcast.EndsAtUtc,
                broadcast.AudienceFilterJson
            }),
            severity: AuditSeverity.Information,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        // Best-effort: schedule deactivation at EndsAtUtc. Failure here
        // doesn't affect the broadcast itself; GetActiveAsync filters
        // by EndsAtUtc anyway, so the worst case is a stale IsActive
        // flag until the next manual cancel.
        try
        {
            await _scheduling.ScheduleOnceAsync(
                DeactivateJobKey,
                broadcast.EndsAtUtc,
                broadcast.Id.ToString(),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to schedule deactivation for broadcast {BroadcastId}; the active-window filter will still drop it at EndsAtUtc.",
                broadcast.Id);
        }

        InvalidateActiveCache();
        return broadcast.Id;
    }

    public async Task<IReadOnlyList<Broadcast>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Broadcasts
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.StartsAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Broadcast>> GetActiveAsync(
        string roleArea,
        CancellationToken ct = default)
    {
        var key = ActiveCacheKeyPrefix + (roleArea ?? string.Empty);
        if (_cache.TryGetValue<IReadOnlyList<Broadcast>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var now = DateTime.UtcNow;
        var rows = await _db.Broadcasts
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                        && b.IsActive
                        && b.StartsAtUtc <= now
                        && b.EndsAtUtc > now)
            .OrderBy(b => b.StartsAtUtc)
            .ToListAsync(ct);

        var matched = rows
            .Where(b => AudienceMatches(b.AudienceFilterJson, roleArea ?? string.Empty))
            .ToList();

        IReadOnlyList<Broadcast> result = matched;
        _cache.Set(key, result, ActiveCacheTtl);
        return result;
    }

    public async Task<bool> CancelAsync(Guid id, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var broadcast = await _db.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);
        if (broadcast is null) return false;

        broadcast.IsActive = false;

        var actorId = actor.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        await _audit.WriteAsync(
            action: "Broadcast.Cancelled",
            entityType: nameof(Broadcast),
            entityId: broadcast.Id.ToString(),
            actorUserId: string.IsNullOrEmpty(actorId) ? null : actorId,
            detailsJson: JsonSerializer.Serialize(new { broadcast.Subject }),
            severity: AuditSeverity.Information,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        InvalidateActiveCache();
        return true;
    }

    // ----- helpers ------------------------------------------------------

    /// <summary>
    /// Returns <c>true</c> when the audience filter explicitly includes
    /// the given role area, "all", or has no <c>roles</c> key at all
    /// (treat-as-everyone fallback).
    /// </summary>
    internal static bool AudienceMatches(string audienceFilterJson, string roleArea)
    {
        if (string.IsNullOrWhiteSpace(audienceFilterJson)) return true;

        try
        {
            using var document = JsonDocument.Parse(audienceFilterJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return true;

            if (!document.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            // Empty array → no audience explicitly chosen → render to nobody.
            if (roles.GetArrayLength() == 0) return false;

            foreach (var item in roles.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) continue;
                var value = item.GetString();
                if (string.IsNullOrEmpty(value)) continue;
                if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(value, roleArea, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        catch (JsonException)
        {
            // Malformed JSON: fail closed so we never broadcast to
            // somebody we didn't intend to.
            return false;
        }
    }

    private void InvalidateActiveCache()
    {
        // The four canonical role areas plus an "unknown" fallback key.
        foreach (var area in new[] { "SuperAdmin", "Institute", "Teacher", "Student", "" })
        {
            _cache.Remove(ActiveCacheKeyPrefix + area);
        }
    }
}
