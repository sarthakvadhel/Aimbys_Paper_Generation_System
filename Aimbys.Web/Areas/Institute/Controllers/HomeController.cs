using Aimbys.Application.Authorization;
using Aimbys.Application.Dashboard;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Landing surface for an Institute Administrator.
///
/// <para>
/// Slice A: KPI tiles, charts and tables are all backed by
/// <see cref="IDashboardService.GetInstituteSnapshotAsync"/>. Tenancy is
/// resolved through <see cref="IInstituteScope"/>; if the signed-in
/// user has no resolvable institute the request is forbidden.
/// </para>
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly IInstituteScope _scope;

    public HomeController(IDashboardService dashboard, IInstituteScope scope)
    {
        _dashboard = dashboard;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var snapshot = await _dashboard.GetInstituteSnapshotAsync(instituteId.Value, ct);
        return View(snapshot);
    }

    /// <summary>7-day papers + exams activity feed.</summary>
    [HttpGet]
    public async Task<IActionResult> WeeklyActivityData(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var snapshot = await _dashboard.GetInstituteSnapshotAsync(instituteId.Value, ct);
        return Json(ToChartJson(snapshot.WeeklyActivity));
    }

    /// <summary>Per-subject average + pass-rate feed for published results.</summary>
    [HttpGet]
    public async Task<IActionResult> SubjectPerformanceData(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var snapshot = await _dashboard.GetInstituteSnapshotAsync(instituteId.Value, ct);
        return Json(ToChartJson(snapshot.SubjectPerformance));
    }

    private static object ToChartJson(ChartFeed feed) => new
    {
        labels = feed.Labels,
        datasets = feed.Series
            .Select(s => new { label = s.Label, data = s.Data })
            .ToArray()
    };
}
