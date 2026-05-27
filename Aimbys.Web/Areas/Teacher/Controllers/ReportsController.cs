using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class ReportsController : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult ChartData(string metricKey)
    {
        // V1: Return seed data for class performance summary
        return metricKey switch
        {
            "ClassAvg" => Json(new
            {
                labels = new[] { "XII-A", "XII-B", "XI-A", "XI-B", "X-C" },
                datasets = new object[]
                {
                    new { label = "Avg Score (%)", data = new[] { 74, 68, 71, 65, 78 } }
                }
            }),
            "SubjectMastery" => Json(new
            {
                labels = new[] { "Algebra", "Calculus", "Geometry", "Statistics", "Trigonometry" },
                datasets = new object[]
                {
                    new { label = "Mastery (%)", data = new[] { 78, 65, 82, 70, 74 } }
                }
            }),
            _ => Json(new { labels = Array.Empty<string>(), datasets = Array.Empty<object>() })
        };
    }
}
