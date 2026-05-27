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
public class StreamsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IOrgTreeService _orgTree;

    public StreamsController(AppDbContext db, IInstituteScope scope, IOrgTreeService orgTree)
    {
        _db = db;
        _scope = scope;
        _orgTree = orgTree;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var streams = await _db.Streams
            .Where(s => s.InstituteId == instituteId.Value)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return View(streams);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        await _orgTree.CreateStreamAsync(instituteId.Value, name.Trim(), description?.Trim(), ct);
        TempData["Success"] = "Stream created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string? description, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var stream = await _db.Streams.FirstOrDefaultAsync(s => s.Id == id && s.InstituteId == instituteId.Value, ct);
        if (stream is null) return NotFound();

        stream.Name = name.Trim();
        stream.Description = description?.Trim();
        stream.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Stream updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var stream = await _db.Streams.FirstOrDefaultAsync(s => s.Id == id && s.InstituteId == instituteId.Value, ct);
        if (stream is null) return NotFound();

        stream.IsDeleted = true;
        stream.DeletedAtUtc = DateTime.UtcNow;
        stream.DeletedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Stream deleted.";
        return RedirectToAction(nameof(Index));
    }
}
