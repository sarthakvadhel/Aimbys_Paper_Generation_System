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
/// Institute-admin paper management surface. Admins view all papers,
/// approve submitted papers, and return them with comments.
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

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var papers = await _db.Papers
            .Where(p => p.InstituteId == instituteId.Value)
            .Include(p => p.Versions)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(ct);

        return View(papers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _paperService.ApproveAsync(id, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "Failed to approve paper.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Paper approved.";
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

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "Failed to return paper.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Paper returned to author.";
        return RedirectToAction(nameof(Index));
    }
}
