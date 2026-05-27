using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class LeaderboardController : Controller
{
    /// <summary>
    /// V1: Renders a simple rank table with seed data.
    /// In production, reads CachedLeaderboardEntry if available and
    /// respects LeaderboardVisibilityPolicy.
    /// </summary>
    public IActionResult Index()
    {
        // Seed data for V1 leaderboard
        var entries = new[]
        {
            new LeaderboardEntry(1, "You", 96.2),
            new LeaderboardEntry(2, "Student B", 94.8),
            new LeaderboardEntry(3, "Student C", 93.1),
            new LeaderboardEntry(4, "Student D", 91.5),
            new LeaderboardEntry(5, "Student E", 89.7),
            new LeaderboardEntry(6, "Student F", 87.3),
            new LeaderboardEntry(7, "Student G", 85.9),
            new LeaderboardEntry(8, "Student H", 84.2),
            new LeaderboardEntry(9, "Student I", 82.6),
            new LeaderboardEntry(10, "Student J", 80.1)
        };

        return View(entries);
    }
}

public sealed record LeaderboardEntry(int Rank, string Name, double Percentile);
