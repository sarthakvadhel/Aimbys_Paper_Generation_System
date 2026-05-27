using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Configuration;
using Aimbys.Domain.Entities.Configuration;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aimbys.Infrastructure.Configuration;

/// <summary>
/// Default <see cref="IConfigurationService"/>. Reads come out of an
/// in-process <see cref="IMemoryCache"/> with a 5-minute sliding
/// expiry; writes invalidate the matching key so the next read
/// reflects the change.
///
/// <para>
/// Across multiple web instances the cache eventually converges
/// after the TTL window; callers that need stronger consistency
/// (rare for config reads) can call
/// <see cref="InvalidateAll"/> after a coordinated update or read
/// directly from the DbContext.
/// </para>
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    /// <summary>Sliding cache window. Public so tests can verify the contract.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private const string PlatformPrefix = "config:platform:";
    private const string InstitutePrefix = "config:institute:";
    private const string FeaturePrefix = "config:feature:";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditWriter _audit;

    public ConfigurationService(
        AppDbContext db,
        IMemoryCache cache,
        UserManager<IdentityUser> userManager,
        IAuditWriter audit)
    {
        _db = db;
        _cache = cache;
        _userManager = userManager;
        _audit = audit;
    }

    // ===== Reads =======================================================

    public async Task<T?> GetPlatformAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));

        var cacheKey = PlatformPrefix + key;
        if (_cache.TryGetValue<string>(cacheKey, out var cachedJson) && cachedJson is not null)
        {
            return Deserialize<T>(cachedJson);
        }

        var setting = await _db.Set<PlatformSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            return default;
        }

        _cache.Set(cacheKey, setting.ValueJson, CacheTtl);
        return Deserialize<T>(setting.ValueJson);
    }

    public async Task<T?> GetInstituteAsync<T>(
        Guid instituteId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));

        var cacheKey = $"{InstitutePrefix}{instituteId}:{key}";
        if (_cache.TryGetValue<string>(cacheKey, out var cachedJson) && cachedJson is not null)
        {
            return Deserialize<T>(cachedJson);
        }

        var setting = await _db.Set<InstituteSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.InstituteId == instituteId && s.Key == key,
                cancellationToken);

        if (setting is not null)
        {
            _cache.Set(cacheKey, setting.ValueJson, CacheTtl);
            return Deserialize<T>(setting.ValueJson);
        }

        // Fall through to platform-wide default so callers see
        // "institute override or platform default" without a second call.
        return await GetPlatformAsync<T>(key, cancellationToken);
    }

    public async Task<bool> IsFeatureEnabledAsync(
        string key,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));

        var cacheKey = FeaturePrefix + key;
        FeatureToggleSnapshot? snapshot;

        if (_cache.TryGetValue<FeatureToggleSnapshot>(cacheKey, out var cached) && cached is not null)
        {
            snapshot = cached;
        }
        else
        {
            var row = await _db.Set<FeatureToggle>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);

            // Unknown key: cache a "missing" sentinel so we don't keep
            // round-tripping for the same negative answer.
            snapshot = row is null
                ? FeatureToggleSnapshot.Missing
                : new FeatureToggleSnapshot(row.IsEnabledGlobally, row.InstituteOverridesJson);

            _cache.Set(cacheKey, snapshot, CacheTtl);
        }

        if (snapshot.IsMissing)
        {
            return false;
        }

        if (instituteId is { } id && TryReadOverride(snapshot.OverridesJson, id, out var overrideValue))
        {
            return overrideValue;
        }

        return snapshot.GlobalDefault;
    }

    // ===== Writes ======================================================

    public async Task SetAsync<T>(
        ConfigurationScope scope,
        string key,
        T value,
        ClaimsPrincipal actor,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));

        EnforceWriteRole(scope, actor);

        var json = JsonSerializer.Serialize(value);
        var actorUserId = _userManager.GetUserId(actor);
        var now = DateTime.UtcNow;

        if (scope == ConfigurationScope.Platform)
        {
            var existing = await _db.Set<PlatformSetting>()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (existing is null)
            {
                _db.Set<PlatformSetting>().Add(new PlatformSetting
                {
                    Key = key,
                    ValueJson = json,
                    UpdatedByUserId = actorUserId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existing.ValueJson = json;
                existing.UpdatedByUserId = actorUserId;
                existing.UpdatedAtUtc = now;
            }

            _cache.Remove(PlatformPrefix + key);
        }
        else
        {
            if (instituteId is null)
                throw new ArgumentException(
                    "instituteId is required for institute-scoped writes.",
                    nameof(instituteId));

            var existing = await _db.Set<InstituteSetting>()
                .FirstOrDefaultAsync(
                    s => s.InstituteId == instituteId.Value && s.Key == key,
                    cancellationToken);

            if (existing is null)
            {
                _db.Set<InstituteSetting>().Add(new InstituteSetting
                {
                    InstituteId = instituteId.Value,
                    Key = key,
                    ValueJson = json,
                    UpdatedByUserId = actorUserId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existing.ValueJson = json;
                existing.UpdatedByUserId = actorUserId;
                existing.UpdatedAtUtc = now;
            }

            _cache.Remove($"{InstitutePrefix}{instituteId.Value}:{key}");
        }

        await _audit.WriteAsync(
            "Configuration.Set",
            entityType: scope == ConfigurationScope.Platform ? "PlatformSetting" : "InstituteSetting",
            entityId: instituteId is null ? key : $"{instituteId.Value}:{key}",
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { scope = scope.ToString(), key }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetFeatureAsync(
        string key,
        bool isEnabled,
        ClaimsPrincipal actor,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));

        EnforceWriteRole(
            instituteId is null ? ConfigurationScope.Platform : ConfigurationScope.Institute,
            actor);

        var actorUserId = _userManager.GetUserId(actor);
        var now = DateTime.UtcNow;

        var row = await _db.Set<FeatureToggle>()
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);

        if (row is null)
        {
            row = new FeatureToggle
            {
                Key = key,
                IsEnabledGlobally = instituteId is null && isEnabled,
                InstituteOverridesJson = "{}",
                UpdatedByUserId = actorUserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.Set<FeatureToggle>().Add(row);
        }

        if (instituteId is null)
        {
            row.IsEnabledGlobally = isEnabled;
        }
        else
        {
            // Update the JSON dictionary in place.
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(row.InstituteOverridesJson)
                       ?? new Dictionary<string, bool>();
            dict[instituteId.Value.ToString()] = isEnabled;
            row.InstituteOverridesJson = JsonSerializer.Serialize(dict);
        }

        row.UpdatedByUserId = actorUserId;
        row.UpdatedAtUtc = now;

        _cache.Remove(FeaturePrefix + key);

        await _audit.WriteAsync(
            "Configuration.FeatureToggleSet",
            entityType: "FeatureToggle",
            entityId: instituteId is null ? key : $"{instituteId.Value}:{key}",
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { key, instituteId, isEnabled }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Drops every cached configuration entry. Useful when ops needs
    /// to force a re-read across nodes after a coordinated change.
    /// </summary>
    public void InvalidateAll()
    {
        // IMemoryCache has no "clear" API; we rely on TTL eviction
        // for the rare global-invalidation case. Implementations that
        // need stronger guarantees can switch to MemoryCache (with
        // its Compact API) or Redis.
        if (_cache is MemoryCache mc)
        {
            mc.Clear();
        }
    }

    // ===== helpers =====================================================

    private static void EnforceWriteRole(ConfigurationScope scope, ClaimsPrincipal actor)
    {
        var isSuperAdmin = actor.IsInRole(Roles.SuperAdmin);
        if (scope == ConfigurationScope.Platform && !isSuperAdmin)
        {
            throw new UnauthorizedAccessException("Platform-scoped configuration writes require SuperAdmin.");
        }

        if (scope == ConfigurationScope.Institute
            && !isSuperAdmin
            && !actor.IsInRole(Roles.InstituteAdmin))
        {
            throw new UnauthorizedAccessException(
                "Institute-scoped configuration writes require InstituteAdmin or SuperAdmin.");
        }
    }

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "null")
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json);
    }

    private static bool TryReadOverride(string overridesJson, Guid instituteId, out bool value)
    {
        value = default;
        if (string.IsNullOrEmpty(overridesJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(overridesJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

            // Try multiple casings since Guid.ToString() is lower-case
            // by default but admin tools may write upper-case.
            var lower = instituteId.ToString();
            if (doc.RootElement.TryGetProperty(lower, out var v1)
                && (v1.ValueKind == JsonValueKind.True || v1.ValueKind == JsonValueKind.False))
            {
                value = v1.GetBoolean();
                return true;
            }

            var upper = instituteId.ToString().ToUpperInvariant();
            if (doc.RootElement.TryGetProperty(upper, out var v2)
                && (v2.ValueKind == JsonValueKind.True || v2.ValueKind == JsonValueKind.False))
            {
                value = v2.GetBoolean();
                return true;
            }
        }
        catch (JsonException)
        {
            // Malformed override JSON shouldn't take down the request;
            // fail closed to the global default.
        }

        return false;
    }

    /// <summary>
    /// In-memory snapshot of a feature row for the cache. Carries an
    /// "is missing" sentinel so unknown-key reads can be cached too
    /// (and don't pound the database for absent flags).
    /// </summary>
    private sealed record FeatureToggleSnapshot(bool GlobalDefault, string OverridesJson, bool IsMissing = false)
    {
        public static FeatureToggleSnapshot Missing { get; } = new(false, "{}", IsMissing: true);
    }
}
