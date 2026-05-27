using System.Security.Claims;
using Aimbys.Application.Results;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Student.Controllers;

/// <summary>
/// Student-facing result views and appeal submission.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ResultsController : Controller
{
    private readonly IResultPublicationService _publication;
    private readonly IAppealService _appeals;
    private readonly AppDbContext _db;

    public ResultsController(
        IResultPublicationService publication,
        IAppealService appeals,
        AppDbContext db)
    {
        _publication = publication;
        _appeals = appeals;
        _db = db;
    }

    /// <summary>Published results list for the current student.</summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var results = await _db.Results
            .Where(r => r.IsPublished)
            .OrderByDescending(r => r.PublishedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return View(results);
    }

    /// <summary>Per-answer score breakdown for a specific attempt.</summary>
    public async Task<IActionResult> Details(Guid attemptId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var view = await _publication.GetStudentResultAsync(attemptId, userId, ct);
        if (view is null)
            return NotFound();

        return View(view);
    }

    /// <summary>Appeal form (GET).</summary>
    public IActionResult Appeal(Guid attemptAnswerId)
    {
        ViewBag.AttemptAnswerId = attemptAnswerId;
        return View();
    }

    /// <summary>Submit an appeal (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Appeal(Guid attemptAnswerId, string reason, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var result = await _appeals.FileAppealAsync(attemptAnswerId, userId, reason, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Appeal submitted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
