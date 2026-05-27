using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Landing surface for the Teacher / Examiner role. Real teacher
/// tooling (paper generation, evaluation desk, moderation queues)
/// lands in later chunks; today the dashboard uses the seed data
/// from <c>TeacherDashboard.tsx</c>.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>Per-class average bar chart (XII-A, XII-B, XI-A, …).</summary>
    [HttpGet]
    public IActionResult ClassAvgData()
    {
        return Json(new
        {
            labels = new[] { "XII-A", "XII-B", "XI-A", "XI-B", "X-C" },
            datasets = new object[]
            {
                new { label = "Avg Score (%)", data = new[] { 74, 68, 71, 65, 78 } }
            }
        });
    }
}
