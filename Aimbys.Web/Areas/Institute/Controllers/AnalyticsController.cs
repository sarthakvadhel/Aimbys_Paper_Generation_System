using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin + "," + Roles.Teacher)]
public class AnalyticsController : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult ChartData(string metricKey)
    {
        // V1: Return seed data matching InstituteDashboard.tsx patterns
        // In production, this reads from AnalyticsSnapshot where Scope == Institute
        return metricKey switch
        {
            "WeeklyActivity" => Json(new
            {
                labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                datasets = new object[]
                {
                    new { label = "Papers", data = new[] { 24, 31, 18, 42, 38, 15, 8 } },
                    new { label = "Exams", data = new[] { 8, 12, 6, 14, 11, 4, 2 } }
                }
            }),
            "ClassPerformance" => Json(new
            {
                labels = new[] { "XII-A", "XII-B", "XI-A", "XI-B", "X-C" },
                datasets = new object[]
                {
                    new { label = "Avg Score (%)", data = new[] { 74, 68, 71, 65, 78 } },
                    new { label = "Pass Rate (%)", data = new[] { 88, 82, 85, 79, 91 } }
                }
            }),
            "SubjectTrend" => Json(new
            {
                labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                datasets = new object[]
                {
                    new { label = "Maths", data = new[] { 64, 66, 68, 70, 72, 74 } },
                    new { label = "Physics", data = new[] { 62, 64, 66, 68, 70, 72 } },
                    new { label = "Chemistry", data = new[] { 70, 71, 72, 73, 74, 75 } }
                }
            }),
            "EvaluatorEfficiency" => Json(new
            {
                labels = new[] { "Dr. Sharma", "Ms. Patel", "Mr. Kumar", "Dr. Singh", "Ms. Rao" },
                datasets = new object[]
                {
                    new { label = "Papers/Day", data = new[] { 18, 22, 15, 20, 24 } }
                }
            }),
            _ => Json(new { labels = Array.Empty<string>(), datasets = Array.Empty<object>() })
        };
    }

    [HttpGet]
    public IActionResult Export(string format = "csv")
    {
        var csv = "Metric,Value\nStudents,4820\nTeachers,186\nPapers,1240\nExams,320\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "institute-analytics.csv");
    }
}
