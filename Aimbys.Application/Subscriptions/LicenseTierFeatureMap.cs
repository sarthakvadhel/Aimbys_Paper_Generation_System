using Aimbys.Application.Configuration;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.Subscriptions;

/// <summary>
/// Maps license tiers to allowed features. Used by the Settings page
/// to show which toggles the institute's tier permits.
/// </summary>
public static class LicenseTierFeatureMap
{
    private static readonly IReadOnlyDictionary<string, LicenseTier> MinimumTierForFeature = new Dictionary<string, LicenseTier>
    {
        [PlatformFeatureKeys.CodingExamEnabled] = LicenseTier.Enterprise,
        [PlatformFeatureKeys.MultilingualEnabled] = LicenseTier.Premium,
        [PlatformFeatureKeys.LeaderboardEnabled] = LicenseTier.Premium,
        [PlatformFeatureKeys.ModerationRequired] = LicenseTier.Standard,
        [PlatformFeatureKeys.FullscreenEnforced] = LicenseTier.Standard,
    };

    public static bool IsTierAllowed(string featureKey, LicenseTier instituteTier)
    {
        if (!MinimumTierForFeature.TryGetValue(featureKey, out var required))
            return true; // unknown key = no tier restriction
        return instituteTier >= required;
    }

    public static LicenseTier GetMinimumTier(string featureKey)
    {
        return MinimumTierForFeature.TryGetValue(featureKey, out var tier) ? tier : LicenseTier.Standard;
    }
}
