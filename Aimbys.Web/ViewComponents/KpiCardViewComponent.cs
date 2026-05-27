using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Direction of the KPI's recent change. Drives the colour and arrow
/// glyph rendered next to the headline value.
/// </summary>
public enum KpiTrend
{
    /// <summary>No trend indicator is rendered.</summary>
    None = 0,
    Up = 1,
    Down = 2
}

/// <summary>
/// Renders a single KPI tile &mdash; the rectangular card with an
/// accent-coloured icon block, a big numeric value, a label, an
/// optional sub-label, and an optional trend arrow. Mirrors the
/// `kpis.map(...)` block at the top of every React `*Dashboard.tsx`.
///
/// <para>
/// Invoked from any view via:
/// <c>@await Component.InvokeAsync("KpiCard", new { ... })</c>.
/// </para>
/// </summary>
public class KpiCardViewComponent : ViewComponent
{
    /// <summary>
    /// Renders the tile. <paramref name="iconKey"/> resolves through
    /// <see cref="Aimbys.Web.Navigation.RoleNavIcons"/> so any glyph
    /// already used by the role nav is available here without
    /// duplication. <paramref name="accentHex"/> is treated as a
    /// trusted CSS colour: callers are expected to use safe hex
    /// values.
    /// </summary>
    public IViewComponentResult Invoke(
        string label,
        string value,
        string? sub = null,
        KpiTrend trend = KpiTrend.None,
        string iconKey = "speedometer",
        string accentHex = "#1d4ed8")
    {
        return View("Default", new KpiCardViewModel(
            Label: label,
            Value: value,
            Sub: sub,
            Trend: trend,
            IconKey: iconKey,
            AccentHex: accentHex));
    }
}

public sealed record KpiCardViewModel(
    string Label,
    string Value,
    string? Sub,
    KpiTrend Trend,
    string IconKey,
    string AccentHex);
