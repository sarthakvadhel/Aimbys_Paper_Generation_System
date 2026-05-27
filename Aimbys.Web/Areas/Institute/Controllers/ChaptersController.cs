using Aimbys.Application.Authorization;
using Aimbys.Application.OrgTree;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class ChaptersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IOrgTreeService _orgTree;

    public ChaptersController(AppDbContext db, IInstituteScope scope, IOrgTreeService orgTree)
    {
        _db = db;
        _scope = scope;
        _orgTree = orgTree;
    }

    public async Task<IActionResult> Index(Guid subjectId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var chapters = await _db.Chapters
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        ViewBag.SubjectId = subjectId;
        return View(chapters);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid subjectId, string title, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Title is required.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        await _orgTree.CreateChapterAsync(subjectId, title.Trim(), description?.Trim(), ct);
        TempData["Success"] = "Chapter created.";
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid subjectId, [FromBody] List<Guid> orderedIds, CancellationToken ct)
    {
        if (orderedIds is null || orderedIds.Count == 0)
            return BadRequest("No chapter IDs provided.");

        await _orgTree.ReorderChaptersAsync(subjectId, orderedIds, ct);
        return Json(new { success = true });
    }
}
