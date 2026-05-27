namespace Aimbys.Domain.Entities.Configuration;

/// <summary>
/// Boolean feature flag with a global default plus optional per-institute
/// overrides. Reads always go through <c>IConfigurationService.IsFeatureEnabledAsync</c>:
///
/// <list type="number">
///   <item>If an institute id is provided and the institute has an
///         override in <see cref="InstituteOverridesJson"/>, that
///         value wins.</item>
///   <item>Otherwise <see cref="IsEnabledGlobally"/> applies.</item>
/// </list>
///
/// <para>
/// Overrides are stored as a JSON dictionary
/// (<c>{"&lt;institute-guid&gt;": true|false, ...}</c>) so a single
/// flag carries any number of overrides without proliferating rows.
/// </para>
/// </summary>
public class FeatureToggle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable feature key. Predefined keys live in
    /// <c>Aimbys.Application.Configuration.PlatformFeatureKeys</c>.
    /// Globally unique.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public bool IsEnabledGlobally { get; set; }

    /// <summary>
    /// JSON dict of per-institute overrides:
    /// <c>{"6f9a-...": true, "abc1-...": false}</c>. Defaults to an
    /// empty object so reads can deserialize unconditionally.
    /// </summary>
    public string InstituteOverridesJson { get; set; } = "{}";

    public string? Description { get; set; }

    public string? UpdatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
