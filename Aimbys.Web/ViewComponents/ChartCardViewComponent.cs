using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Renders a Bootstrap card with a header (title + optional subtitle)
/// and a <c>&lt;canvas&gt;</c> tag that the client-side
/// <c>charts.js</c> helper populates by fetching JSON from
/// <see cref="ChartCardViewModel.DataUrl"/>.
///
/// <para>
/// V1 endpoints return hardcoded seed data; later chunks swap in
/// real EF queries by changing nothing more than the controller
/// implementation behind the URL.
/// </para>
/// </summary>
public class ChartCardViewComponent : ViewComponent
{
    /// <summary>
    /// Builds the card. <paramref name="chartType"/> matches Chart.js
    /// type names (<c>line</c> / <c>bar</c> / <c>area</c> / <c>pie</c>
    /// / <c>doughnut</c>). <paramref name="height"/> is the canvas
    /// pixel height; the width follows the container.
    /// </summary>
    public IViewComponentResult Invoke(
        string title,
        string dataUrl,
        string chartType = "line",
        string? subtitle = null,
        int height = 240,
        string? canvasId = null)
    {
        return View("Default", new ChartCardViewModel(
            Title: title,
            Subtitle: subtitle,
            DataUrl: dataUrl,
            ChartType: chartType,
            Height: height,
            CanvasId: canvasId ?? "aimbys-chart-" + Guid.NewGuid().ToString("N")[..8]));
    }
}

public sealed record ChartCardViewModel(
    string Title,
    string? Subtitle,
    string DataUrl,
    string ChartType,
    int Height,
    string CanvasId);
