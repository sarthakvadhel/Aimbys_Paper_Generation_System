using System.Security.Claims;
using Aimbys.Application.Authorization;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class ClassBatchesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public ClassBatchesController(AppDbContext db, IInstituteScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);

        // InstituteAdmin fallback: resolve directly from TeacherProfile
        if (instituteId is null)
        {
            instituteId = await _db.TeacherProfiles
                .Where(t => t.UserId == userId)
                .Select(t => (Guid?)t.InstituteId)
                .FirstOrDefaultAsync(ct);
        }

        if (instituteId is null)
        {
            TempData["Error"] = "Could not resolve your institute. Make sure your account has a profile.";
            return RedirectToAction("Index", "Home");
        }

        var batches = await _db.ClassBatches
            .Where(cb => cb.InstituteId == instituteId.Value)
            .Include(cb => cb.AcademicYear)
            .OrderBy(cb => cb.Name)
            .ToListAsync(ct);

        var academicYears = await _db.AcademicYears
            .Where(y => y.InstituteId == instituteId.Value)
            .OrderByDescending(y => y.StartDate)
            .ToListAsync(ct);

        ViewBag.AcademicYears = academicYears;
        return View(batches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, Guid academicYearId, Guid? departmentId, Guid? streamId, string? gradeLevel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct)
            ?? await _db.TeacherProfiles.Where(t => t.UserId == userId).Select(t => (Guid?)t.InstituteId).FirstOrDefaultAsync(ct);

        if (instituteId is null) return Forbid();

        var batch = new Domain.Entities.ClassBatch
        {
            InstituteId = instituteId.Value,
            Name = name.Trim(),
            AcademicYearId = academicYearId,
            DepartmentId = departmentId,
            StreamId = streamId,
            GradeLevel = gradeLevel?.Trim()
        };

        _db.ClassBatches.Add(batch);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Class batch created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, Guid academicYearId, Guid? departmentId, Guid? streamId, string? gradeLevel, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var batch = await _db.ClassBatches.FirstOrDefaultAsync(cb => cb.Id == id && cb.InstituteId == instituteId.Value, ct);
        if (batch is null) return NotFound();

        batch.Name = name.Trim();
        batch.AcademicYearId = academicYearId;
        batch.DepartmentId = departmentId;
        batch.StreamId = streamId;
        batch.GradeLevel = gradeLevel?.Trim();
        batch.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Class batch updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var batch = await _db.ClassBatches.FirstOrDefaultAsync(cb => cb.Id == id && cb.InstituteId == instituteId.Value, ct);
        if (batch is null) return NotFound();

        _db.ClassBatches.Remove(batch);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Class batch deleted.";
        return RedirectToAction(nameof(Index));
    }
}
