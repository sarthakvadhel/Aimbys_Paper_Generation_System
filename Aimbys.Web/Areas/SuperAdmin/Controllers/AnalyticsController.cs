using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class AnalyticsController : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult ChartData(string metricKey)
    {
        // V1: Return seed data matching the React GlobalAnalytics.tsx patterns
        // In production, this reads from AnalyticsSnapshot where Scope == Platform
        return metricKey switch
        {
            "DailyActivity" => Json(new
            {
                labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                datasets = new object[]
                {
                    new { label = "Logins", data = new[] { 12400, 15800, 18200, 22400, 28600, 32100 } },
                    new { label = "Exams", data = new[] { 420, 580, 740, 920, 1100, 1340 } }
                }
            }),
            "SubjectBreakdown" => Json(new
            {
                labels = new[] { "Maths", "Physics", "Chemistry", "English", "CS", "Biology" },
                datasets = new object[] { new { label = "Papers", data = new[] { 1840, 1420, 1380, 980, 860, 720 } } }
            }),
            "QuestionTypeDist" => Json(new
            {
                labels = new[] { "MCQ", "Descriptive", "Coding", "FillBlanks", "TrueFalse", "TITA" },
                datasets = new object[] { new { label = "Count", data = new[] { 42000, 28000, 8400, 12000, 9800, 4200 } } }
            }),
            "PassFailTrend" => Json(new
            {
                labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                datasets = new object[]
                {
                    new { label = "Pass Rate %", data = new[] { 72, 74, 73, 76, 78, 80 } },
                    new { label = "Fail Rate %", data = new[] { 28, 26, 27, 24, 22, 20 } }
                }
            }),
            _ => Json(new { labels = Array.Empty<string>(), datasets = Array.Empty<object>() })
        };
    }

    [HttpGet]
    public IActionResult Export(string format = "csv")
    {
        var csv = "Metric,Value\nInstitutes,2418\nUsers,1284302\nPapers,851240\nExams,42000\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "platform-analytics.csv");
    }
}
