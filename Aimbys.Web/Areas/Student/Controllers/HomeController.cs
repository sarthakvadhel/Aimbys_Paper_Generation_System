using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Student.Controllers;

/// <summary>
/// Landing surface for the Student / Candidate role. Real student
/// tooling (exam attempts, results detail, certificates, transcripts)
/// lands in later chunks; today the dashboard uses the seed data
/// from <c>StudentDashboard.tsx</c>.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>Subject mastery bar chart for the recent-results panel.</summary>
    [HttpGet]
    public IActionResult SubjectProgressData()
    {
        return Json(new
        {
            labels = new[] { "Mathematics", "Physics", "Chemistry", "English", "Computer Sci." },
            datasets = new object[]
            {
                new { label = "Average (%)", data = new[] { 68.5, 75.5, 75.0, 87.0, 91.7 } }
            }
        });
    }
}
