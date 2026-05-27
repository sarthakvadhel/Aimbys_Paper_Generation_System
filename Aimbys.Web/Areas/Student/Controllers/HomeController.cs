using Aimbys.Application.Dashboard;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Student.Controllers;

/// <summary>
/// Landing surface for the Student / Candidate role.
///
/// <para>
/// Slice A: KPI tiles, the next-exam banner, recent results and the
/// subject-progress chart all flow from
/// <see cref="IDashboardService.GetStudentSnapshotAsync"/>. The service
/// resolves the active <c>StudentProfile</c> from the Identity user id
/// and scopes every query to that student.
/// </para>
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
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
        var snapshot = await _dashboard.GetStudentSnapshotAsync(userId, ct);
        return View(snapshot);
    }

    /// <summary>Per-subject running average for the signed-in student.</summary>
    [HttpGet]
    public async Task<IActionResult> SubjectProgressData(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var snapshot = await _dashboard.GetStudentSnapshotAsync(userId, ct);
        return Json(new
        {
            labels = snapshot.SubjectProgress.Labels,
            datasets = snapshot.SubjectProgress.Series
                .Select(s => new { label = s.Label, data = s.Data })
                .ToArray()
        });
    }
}
