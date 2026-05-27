using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class AnalyticsController : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult ChartData(string metricKey)
    {
        // V1: Return seed data for personal progression
        return metricKey switch
        {
            "SubjectProgress" => Json(new
            {
                labels = new[] { "Mathematics", "Physics", "Chemistry", "English", "Computer Sci." },
                datasets = new object[]
                {
                    new { label = "Current (%)", data = new[] { 72, 68, 75, 87, 92 } },
                    new { label = "Target (%)", data = new[] { 80, 75, 80, 90, 95 } }
                }
            }),
            "ScoreTrend" => Json(new
            {
                labels = new[] { "Test 1", "Test 2", "Test 3", "Test 4", "Test 5", "Test 6" },
                datasets = new object[]
                {
                    new { label = "Score (%)", data = new[] { 62, 68, 65, 74, 78, 82 } }
                }
            }),
            _ => Json(new { labels = Array.Empty<string>(), datasets = Array.Empty<object>() })
        };
    }
}
