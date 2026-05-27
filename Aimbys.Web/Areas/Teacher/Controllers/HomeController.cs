using Aimbys.Application.Dashboard;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Landing surface for the Teacher / Examiner role.
///
/// <para>
/// Slice A: KPIs, charts and tables are all backed by
/// <see cref="IDashboardService.GetTeacherSnapshotAsync"/> &mdash; the
/// service resolves the active <c>TeacherProfile</c> from the Identity
/// user id and returns a teacher-scoped snapshot (own papers, own
/// evaluations, class averages for exams whose paper this teacher
/// authored).
/// </para>
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(IDashboardService dashboard, UserManager<IdentityUser> userManager)
    {
        _dashboard = dashboard;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var snapshot = await _dashboard.GetTeacherSnapshotAsync(userId, ct);
        return View(snapshot);
    }

    /// <summary>Per-class average score chart (published results only).</summary>
    [HttpGet]
    public async Task<IActionResult> ClassAvgData(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var snapshot = await _dashboard.GetTeacherSnapshotAsync(userId, ct);
        return Json(new
        {
            labels = snapshot.ClassAverageScores.Labels,
            datasets = snapshot.ClassAverageScores.Series
                .Select(s => new { label = s.Label, data = s.Data })
                .ToArray()
        });
    }
}
