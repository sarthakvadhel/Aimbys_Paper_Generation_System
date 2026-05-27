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
public class AcademicYearsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IOrgTreeService _orgTree;

    public AcademicYearsController(AppDbContext db, IInstituteScope scope, IOrgTreeService orgTree)
    {
        _db = db;
        _scope = scope;
        _orgTree = orgTree;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var years = await _db.AcademicYears
            .Where(y => y.InstituteId == instituteId.Value)
            .OrderByDescending(y => y.StartDate)
            .ToListAsync(ct);

        return View(years);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var year = new Domain.Entities.AcademicYear
        {
            InstituteId = instituteId.Value,
            Name = name.Trim(),
            StartDate = startDate,
            EndDate = endDate
        };

        _db.AcademicYears.Add(year);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Academic year created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var year = await _db.AcademicYears.FirstOrDefaultAsync(y => y.Id == id && y.InstituteId == instituteId.Value, ct);
        if (year is null) return NotFound();

        year.Name = name.Trim();
        year.StartDate = startDate;
        year.EndDate = endDate;
        year.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Academic year updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrent(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        await _orgTree.SetCurrentAcademicYearAsync(instituteId.Value, id, ct);
        TempData["Success"] = "Current academic year updated.";
        return RedirectToAction(nameof(Index));
    }
}
