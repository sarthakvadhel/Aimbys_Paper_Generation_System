using Aimbys.Application.Authorization;
using Aimbys.Application.Papers;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Institute-admin paper management surface. Drives the full lifecycle
/// (Submit / Approve / Return / Publish / Archive) through
/// <see cref="IPaperAssemblyService"/>; status mutations never happen
/// in the controller.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class PapersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IPaperAssemblyService _paperService;

    public PapersController(
        AppDbContext db,
        IInstituteScope scope,
        IPaperAssemblyService paperService)
    {
        _db = db;
        _scope = scope;
        _paperService = paperService;
    }

    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        // Status totals for the filter chips (one query, computed in memory).
        var counts = await _db.Papers
            .AsNoTracking()
            .Where(p => p.InstituteId == instituteId.Value)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        ViewBag.Counts = Enum.GetValues<PaperStatus>()
            .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
        ViewBag.ActiveFilter = status;

        var query = _db.Papers
            .AsNoTracking()
            .Include(p => p.Versions)
            .Where(p => p.InstituteId == instituteId.Value);

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<PaperStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(p => p.Status == parsed);
        }

        var papers = await query
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(ct);

        return View(papers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _paperService.ApproveAsync(id, User, ct);
        SetFlash(result, "Paper approved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id, string comment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "A comment is required when returning a paper.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _paperService.ReturnAsync(id, User, comment.Trim(), ct);
        SetFlash(result, "Paper returned to author.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await _paperService.PublishAsync(id, User, ct);
        SetFlash(result, "Paper published. It is now available for exam scheduling.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await _paperService.ArchiveAsync(id, User, ct);
        SetFlash(result, "Paper archived.");
        return RedirectToAction(nameof(Index));
    }

    private void SetFlash(PaperResult result, string successMessage)
    {
        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "Action could not be completed.";
        }
        else
        {
            TempData["Success"] = successMessage;
        }
    }
}
