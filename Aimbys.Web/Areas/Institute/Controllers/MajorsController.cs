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
public class MajorsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IOrgTreeService _orgTree;

    public MajorsController(AppDbContext db, IInstituteScope scope, IOrgTreeService orgTree)
    {
        _db = db;
        _scope = scope;
        _orgTree = orgTree;
    }

    public async Task<IActionResult> Index(Guid? streamId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var query = _db.Majors
            .Where(m => m.InstituteId == instituteId.Value);

        if (streamId.HasValue)
            query = query.Where(m => m.StreamId == streamId.Value);

        var majors = await query.OrderBy(m => m.Name).ToListAsync(ct);
        ViewBag.StreamId = streamId;
        return View(majors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid streamId, string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index), new { streamId });
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        await _orgTree.CreateMajorAsync(instituteId.Value, streamId, name.Trim(), description?.Trim(), ct);
        TempData["Success"] = "Major created.";
        return RedirectToAction(nameof(Index), new { streamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid streamId, string name, string? description, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var major = await _db.Majors.FirstOrDefaultAsync(m => m.Id == id && m.InstituteId == instituteId.Value, ct);
        if (major is null) return NotFound();

        major.Name = name.Trim();
        major.Description = description?.Trim();
        major.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Major updated.";
        return RedirectToAction(nameof(Index), new { streamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid streamId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var major = await _db.Majors.FirstOrDefaultAsync(m => m.Id == id && m.InstituteId == instituteId.Value, ct);
        if (major is null) return NotFound();

        major.IsDeleted = true;
        major.DeletedAtUtc = DateTime.UtcNow;
        major.DeletedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Major deleted.";
        return RedirectToAction(nameof(Index), new { streamId });
    }
}
