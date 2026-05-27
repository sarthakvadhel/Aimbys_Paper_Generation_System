using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Landing surface for an Institute Administrator. The dashboard view
/// consumes the Chunk 16 view components; chart endpoints below
/// return seed data sourced directly from
/// <c>InstituteDashboard.tsx</c>.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>Weekly activity area chart (papers + exams per day).</summary>
    [HttpGet]
    public IActionResult WeeklyActivityData()
    {
        return Json(new
        {
            labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
            datasets = new object[]
            {
                new { label = "Papers", data = new[] { 24, 31, 18, 42, 38, 15, 8 } },
                new { label = "Exams",  data = new[] { 8,  12, 6,  14, 11, 4,  2 } }
            }
        });
    }

    /// <summary>Subject performance — average score and pass rate per subject.</summary>
    [HttpGet]
    public IActionResult SubjectPerformanceData()
    {
        return Json(new
        {
            labels = new[] { "Maths", "Physics", "Chemistry", "English", "Biology", "CS" },
            datasets = new object[]
            {
                new { label = "Avg Score (%)", data = new[] { 64, 68, 71, 82, 74, 78 } },
                new { label = "Pass Rate (%)",  data = new[] { 78, 81, 84, 92, 87, 89 } }
            }
        });
    }
}
