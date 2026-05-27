using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities.Notifications;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="INotificationPreferenceService"/>. Reads are
/// cached in <see cref="IMemoryCache"/> with a 5-minute sliding TTL
/// because <see cref="ShouldDeliverAsync"/> is invoked on every
/// notification fan-out and would otherwise hammer the DB.
///
/// <para>
/// "No row" means "default opt-in" so users don't have to seed any
/// rows to receive notifications &mdash; only opt-outs need explicit
/// storage. The cache stores either the
/// <see cref="NotificationPreference"/> snapshot or a
/// <see cref="PreferenceSnapshot.Default"/> sentinel for the absent
/// case.
/// </para>
/// </summary>
public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    /// <summary>Sliding cache window for preference lookups.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private const string CacheKeyPrefix = "notif-pref:";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public NotificationPreferenceService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<NotificationPreference>();

        return await _db.Set<NotificationPreference>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ChannelKey)
            .ToListAsync(cancellationToken);
    }

    public async Task SetPreferenceAsync(
        string userId,
        string channelKey,
        bool isEnabled,
        NotificationSeverity minimumSeverity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(channelKey))
            throw new ArgumentException("channelKey is required.", nameof(channelKey));

        var existing = await _db.Set<NotificationPreference>()
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.ChannelKey == channelKey,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            _db.Set<NotificationPreference>().Add(new NotificationPreference
            {
                UserId = userId,
                ChannelKey = channelKey,
                IsEnabled = isEnabled,
                MinimumSeverity = minimumSeverity,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.IsEnabled = isEnabled;
            existing.MinimumSeverity = minimumSeverity;
            existing.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Drop any cached snapshot for the (user, channel) pair so the
        // next ShouldDeliverAsync call sees the new value.
        _cache.Remove(BuildCacheKey(userId, channelKey));
    }

    public async Task<bool> ShouldDeliverAsync(
        string userId,
        string channelKey,
        NotificationSeverity severity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(channelKey))
        {
            // Defensive: an unknown user / channel can't have an opt-out
            // stored, so default to delivering.
            return true;
        }

        var snapshot = await GetSnapshotAsync(userId, channelKey, cancellationToken);

        if (snapshot.IsDefault)
        {
            return true; // opt-in by default
        }

        if (!snapshot.IsEnabled)
        {
            return false;
        }

        return (int)severity >= (int)snapshot.MinimumSeverity;
    }

    // ----- helpers ------------------------------------------------------

    private async Task<PreferenceSnapshot> GetSnapshotAsync(
        string userId,
        string channelKey,
        CancellationToken ct)
    {
        var cacheKey = BuildCacheKey(userId, channelKey);
        if (_cache.TryGetValue<PreferenceSnapshot>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var row = await _db.Set<NotificationPreference>()
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ChannelKey == channelKey)
            .Select(p => new PreferenceSnapshot(false, p.IsEnabled, p.MinimumSeverity))
            .FirstOrDefaultAsync(ct);

        var snapshot = row ?? PreferenceSnapshot.Default;
        _cache.Set(cacheKey, snapshot, CacheTtl);
        return snapshot;
    }

    private static string BuildCacheKey(string userId, string channelKey) =>
        CacheKeyPrefix + userId + ":" + channelKey;

    /// <summary>
    /// Cached projection of one (userId, channelKey) preference row.
    /// <see cref="IsDefault"/> distinguishes "no row stored" from
    /// "row stored with IsEnabled=true" so future logic that treats
    /// implicit and explicit opt-in differently has the data it needs.
    /// </summary>
    private sealed record PreferenceSnapshot(
        bool IsDefault,
        bool IsEnabled,
        NotificationSeverity MinimumSeverity)
    {
        public static PreferenceSnapshot Default { get; } = new(true, true, NotificationSeverity.Information);
    }
}
