using Aimbys.Application.Authorization;
using Aimbys.Domain.Entities.Blueprints;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class CompetenciesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public CompetenciesController(AppDbContext db, IInstituteScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var competencies = await _db.Competencies
            .Where(c => c.InstituteId == instituteId.Value)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return View(competencies);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, Guid? parentCompetencyId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var maxSort = await _db.Competencies
            .Where(c => c.InstituteId == instituteId.Value)
            .MaxAsync(c => (int?)c.SortOrder, ct) ?? 0;

        var competency = new Competency
        {
            InstituteId = instituteId.Value,
            Name = name.Trim(),
            Code = code?.Trim(),
            ParentCompetencyId = parentCompetencyId,
            SortOrder = maxSort + 1
        };

        _db.Competencies.Add(competency);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Competency created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string? code, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var competency = await _db.Competencies
            .FirstOrDefaultAsync(c => c.Id == id && c.InstituteId == instituteId.Value, ct);

        if (competency is null) return NotFound();

        competency.Name = name.Trim();
        competency.Code = code?.Trim();
        competency.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Competency updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var competency = await _db.Competencies
            .FirstOrDefaultAsync(c => c.Id == id && c.InstituteId == instituteId.Value, ct);

        if (competency is null) return NotFound();

        competency.IsDeleted = true;
        competency.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Competency deleted.";
        return RedirectToAction(nameof(Index));
    }
}
