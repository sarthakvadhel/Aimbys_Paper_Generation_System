using System.Security.Claims;

namespace Aimbys.Application.Configuration;

/// <summary>
/// Scope of a configuration write. Drives the role gate:
/// <see cref="Platform"/> requires SuperAdmin,
/// <see cref="Institute"/> requires InstituteAdmin (or SuperAdmin).
/// </summary>
public enum ConfigurationScope
{
    Platform = 0,
    Institute = 1
}

/// <summary>
/// Single sanctioned route for reading and writing the central
/// configuration store (platform-wide settings, per-institute
/// settings, and feature toggles).
///
/// <para>
/// Reads are cached in <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
/// with a 5-minute TTL; writes invalidate the matching cache key so
/// the next read picks up the new value. Setting two layers of
/// cache + DB keeps the hot path off SQL Server while still
/// guaranteeing eventual consistency across nodes after the TTL
/// expires.
/// </para>
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Reads a platform-wide setting. Returns <c>default(T)</c> when the
    /// key is unset; throws <see cref="System.Text.Json.JsonException"/>
    /// when the stored value can't be deserialized as <typeparamref name="T"/>.
    /// </summary>
    Task<T?> GetPlatformAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a per-institute setting. Falls through to
    /// <see cref="GetPlatformAsync{T}"/> when no institute-scoped row
    /// exists, so callers see "institute override or platform default".
    /// </summary>
    Task<T?> GetInstituteAsync<T>(Guid instituteId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a feature flag. With <paramref name="instituteId"/>
    /// supplied: per-institute override wins, falling back to the
    /// global default. Without: just the global default.
    /// Unknown keys return <c>false</c>.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(string key, Guid? instituteId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a configuration value. Enforces the role gate:
    /// <list type="bullet">
    ///   <item><see cref="ConfigurationScope.Platform"/> + non-SuperAdmin actor &mdash; throws.</item>
    ///   <item><see cref="ConfigurationScope.Institute"/> + non-(InstituteAdmin / SuperAdmin) actor &mdash; throws.</item>
    /// </list>
    /// </summary>
    Task SetAsync<T>(
        ConfigurationScope scope,
        string key,
        T value,
        ClaimsPrincipal actor,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles a feature globally or for a specific institute. Same
    /// role gate as <see cref="SetAsync{T}"/>: institute-scoped
    /// changes need InstituteAdmin or SuperAdmin; global changes need
    /// SuperAdmin. Creates the underlying
    /// <c>FeatureToggle</c> row if missing.
    /// </summary>
    Task SetFeatureAsync(
        string key,
        bool isEnabled,
        ClaimsPrincipal actor,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default);
}
