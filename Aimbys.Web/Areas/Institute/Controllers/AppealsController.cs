using Aimbys.Application.Results;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Institute-level appeal review interface.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class AppealsController : Controller
{
    private readonly IAppealService _appeals;
    private readonly AppDbContext _db;

    public AppealsController(IAppealService appeals, AppDbContext db)
    {
        _appeals = appeals;
        _db = db;
    }

    /// <summary>Open appeals list.</summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var appeals = await _db.ResultAppeals
            .OrderByDescending(a => a.FiledAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return View(appeals);
    }

    /// <summary>Review form for a single appeal.</summary>
    public async Task<IActionResult> Review(Guid id, CancellationToken ct)
    {
        var appeal = await _db.ResultAppeals.FindAsync(new object[] { id }, ct);
        if (appeal is null) return NotFound();
        return View(appeal);
    }

    /// <summary>Uphold the original score.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Uphold(Guid id, string? comment, CancellationToken ct)
    {
        var result = await _appeals.UpholdAsync(id, User, comment, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Appeal upheld — original score retained.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Adjust the score.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(Guid id, decimal newScore, string reason, CancellationToken ct)
    {
        var result = await _appeals.AdjustAsync(id, User, newScore, reason, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Appeal adjusted — new score recorded.";
        return RedirectToAction(nameof(Index));
    }
}
