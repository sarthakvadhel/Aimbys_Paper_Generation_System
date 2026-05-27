using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// Landing surface for platform-support staff. The dashboard view
/// (<c>Index</c>) consumes the KPI / Chart / DataTable view components
/// shipped in Chunk 16; the V1 chart endpoints below return hardcoded
/// seed data copied verbatim from <c>SuperAdminDashboard.tsx</c>.
/// Later chunks swap in real EF queries by replacing each method's
/// body &mdash; the URL contract is the seam.
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Platform-activity area chart (logins / exams / papers across
    /// the day). Mirrors the React `platformActivity` array.
    /// </summary>
    [HttpGet]
    public IActionResult PlatformActivityData()
    {
        var labels = new[] { "00:00", "04:00", "08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00", "22:00" };
        return Json(new
        {
            labels,
            datasets = new object[]
            {
                new { label = "Active Logins",  data = new[] { 320,  80,   2400, 5800, 4200, 6100, 5400, 3200, 1800, 640 } },
                new { label = "Active Exams",   data = new[] { 18,   2,    140,  284,  196,  310,  268,  148,  82,   28  } },
                new { label = "Papers Created", data = new[] { 4,    1,    68,   142,  88,   165,  130,  74,   42,   12  } }
            }
        });
    }

    /// <summary>
    /// Regional table source data &mdash; institutes per state. The
    /// dashboard renders this as a bar chart so reviewers can scan
    /// distribution at a glance; later chunks may swap to a choropleth.
    /// </summary>
    [HttpGet]
    public IActionResult RegionalData()
    {
        return Json(new
        {
            labels = new[] { "Maharashtra", "Karnataka", "UP", "Tamil Nadu", "Gujarat", "Rajasthan", "West Bengal" },
            datasets = new object[]
            {
                new { label = "Institutes", data = new[] { 342, 280, 410, 265, 198, 187, 215 } }
            }
        });
    }

    /// <summary>License-tier mix as a doughnut chart.</summary>
    [HttpGet]
    public IActionResult LicenseData()
    {
        return Json(new
        {
            labels = new[] { "Government", "Private", "Coaching" },
            datasets = new object[]
            {
                new
                {
                    label = "Licenses",
                    data = new[] { 1180, 840, 398 },
                    backgroundColor = new[] { "#1d4ed8", "#7c3aed", "#0369a1" }
                }
            }
        });
    }
}
