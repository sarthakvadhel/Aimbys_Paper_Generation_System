namespace Aimbys.Application.Configuration;

/// <summary>
/// Predefined <see cref="Aimbys.Domain.Entities.Configuration.FeatureToggle"/>
/// keys the platform recognises. Holding them as <c>const</c>
/// strings means every <c>IConfigurationService.IsFeatureEnabledAsync</c>
/// call site is grep-discoverable and a typo is a compile-time
/// failure rather than a quietly-disabled feature.
///
/// <para>
/// Keys are dot-separated, lower-case-camelCase by convention;
/// re-using this prefix discipline lets future flags namespace
/// themselves cleanly.
/// </para>
/// </summary>
public static class PlatformFeatureKeys
{
    public const string CodingExamEnabled    = "feature.codingExam.enabled";
    public const string MultilingualEnabled  = "feature.multilingual.enabled";
    public const string LeaderboardEnabled   = "feature.leaderboard.enabled";
    public const string ModerationRequired   = "feature.moderation.required";
    public const string FullscreenEnforced   = "feature.proctor.fullscreenEnforced";

    /// <summary>
    /// Three-state flag (Public / RoleScoped / Hidden) controlling who
    /// sees published results. Stored as a string in
    /// <c>PlatformSetting</c> rather than a boolean toggle &mdash; reads
    /// go through <c>IConfigurationService.GetInstituteAsync&lt;string&gt;</c>.
    /// </summary>
    public const string ResultVisibilityMode = "config.results.visibilityMode";

    /// <summary>The full set of boolean toggles surfaced to the admin UI by default.</summary>
    public static readonly IReadOnlyList<string> AllToggles = new[]
    {
        CodingExamEnabled,
        MultilingualEnabled,
        LeaderboardEnabled,
        ModerationRequired,
        FullscreenEnforced
    };
}
