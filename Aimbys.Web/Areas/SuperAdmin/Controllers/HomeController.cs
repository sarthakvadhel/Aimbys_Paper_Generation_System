using Aimbys.Application.Dashboard;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// Landing surface for platform-support staff.
///
/// <para>
/// Slice A: the dashboard model and the three chart-feed endpoints all
/// go through <see cref="IDashboardService.GetSuperAdminSnapshotAsync"/>
/// so KPI tiles, tables and charts are computed from the same
/// single source of truth (live <c>AppDbContext</c> queries).
/// </para>
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;

    public HomeController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var snapshot = await _dashboard.GetSuperAdminSnapshotAsync(ct);
        return View(snapshot);
    }

    /// <summary>Hourly platform activity for the past 24 hours.</summary>
    [HttpGet]
    public async Task<IActionResult> PlatformActivityData(CancellationToken ct)
    {
        var snapshot = await _dashboard.GetSuperAdminSnapshotAsync(ct);
        return Json(ToChartJson(snapshot.PlatformActivity));
    }

    /// <summary>Top-N states by institute count.</summary>
    [HttpGet]
    public async Task<IActionResult> RegionalData(CancellationToken ct)
    {
        var snapshot = await _dashboard.GetSuperAdminSnapshotAsync(ct);
        return Json(ToChartJson(snapshot.RegionalDistribution));
    }

    /// <summary>License-tier mix as a doughnut chart.</summary>
    [HttpGet]
    public async Task<IActionResult> LicenseData(CancellationToken ct)
    {
        var snapshot = await _dashboard.GetSuperAdminSnapshotAsync(ct);
        var feed = snapshot.LicenseTierMix;
        // Stable accent colours so the doughnut keeps the same identity
        // across reloads and matches the rest of the platform palette.
        var colours = new[] { "#1d4ed8", "#7c3aed", "#0369a1" };

        return Json(new
        {
            labels = feed.Labels,
            datasets = feed.Series.Select(s => new
            {
                label = s.Label,
                data = s.Data,
                backgroundColor = colours
            }).ToArray()
        });
    }

    /// <summary>
    /// Maps a <see cref="ChartFeed"/> to the Chart.js-compatible JSON
    /// shape <c>{ labels, datasets: [{label, data}, …] }</c>.
    /// </summary>
    private static object ToChartJson(ChartFeed feed) => new
    {
        labels = feed.Labels,
        datasets = feed.Series
            .Select(s => new { label = s.Label, data = s.Data })
            .ToArray()
    };
}
