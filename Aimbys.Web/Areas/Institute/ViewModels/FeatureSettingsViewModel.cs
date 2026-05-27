using Aimbys.Domain.Enums;

namespace Aimbys.Web.Areas.Institute.ViewModels;

public sealed class FeatureSettingsViewModel
{
    public LicenseTier CurrentTier { get; set; }
    public List<FeatureToggleRow> Toggles { get; set; } = new();
}

public sealed class FeatureToggleRow
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsTierAllowed { get; set; }
    public LicenseTier MinimumTier { get; set; }
}
