using Aimbys.Application.Authorization;
using Aimbys.Application.Blueprints;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class BlueprintsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IBlueprintAuthoringService _authoring;

    public BlueprintsController(AppDbContext db, IInstituteScope scope, IBlueprintAuthoringService authoring)
    {
        _db = db;
        _scope = scope;
        _authoring = authoring;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var blueprints = await _db.Blueprints
            .Where(b => b.InstituteId == instituteId.Value)
            .OrderByDescending(b => b.UpdatedAtUtc)
            .ToListAsync(ct);

        return View(blueprints);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        ViewBag.Subjects = await _db.Subjects
            .Where(s => s.InstituteId == instituteId.Value)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, Guid subjectId, Guid? assessmentDesignId, int totalMarks, int durationMinutes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Create));
        }

        var request = new BlueprintCreateRequest(name.Trim(), subjectId, assessmentDesignId, totalMarks, durationMinutes);
        var result = await _authoring.CreateAsync(request, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = "Blueprint created.";
        return RedirectToAction(nameof(Edit), new { id = result.BlueprintId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var blueprint = await _db.Blueprints
            .Include(b => b.Versions)
            .FirstOrDefaultAsync(b => b.Id == id && b.InstituteId == instituteId.Value, ct);

        if (blueprint is null) return NotFound();

        return View(blueprint);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, int totalMarks, int durationMinutes, CancellationToken ct)
    {
        var request = new BlueprintEditRequest(
            totalMarks,
            durationMinutes,
            new List<BlueprintSectionDto>(),
            new List<BlueprintConstraintDto>(),
            new List<BlueprintCohortDto>());

        var result = await _authoring.EditAsync(id, request, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = "Blueprint updated.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await _authoring.PublishAsync(id, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = "Blueprint published.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await _authoring.ArchiveAsync(id, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Blueprint archived.";
        return RedirectToAction(nameof(Index));
    }
}
