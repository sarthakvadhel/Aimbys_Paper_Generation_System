using Aimbys.Application.Authorization;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public SubjectsController(AppDbContext db, IInstituteScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var subjects = await _db.Subjects
            .Where(s => s.InstituteId == instituteId.Value)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return View(subjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? description, Guid? departmentId, Guid? streamId, Guid? majorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var subject = new Domain.Entities.Subject
        {
            InstituteId = instituteId.Value,
            Name = name.Trim(),
            Code = code?.Trim(),
            Description = description?.Trim(),
            DepartmentId = departmentId,
            StreamId = streamId,
            MajorId = majorId
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Subject created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string? code, string? description, Guid? departmentId, Guid? streamId, Guid? majorId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id && s.InstituteId == instituteId.Value, ct);
        if (subject is null) return NotFound();

        subject.Name = name.Trim();
        subject.Code = code?.Trim();
        subject.Description = description?.Trim();
        subject.DepartmentId = departmentId;
        subject.StreamId = streamId;
        subject.MajorId = majorId;
        subject.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id && s.InstituteId == instituteId.Value, ct);
        if (subject is null) return NotFound();

        subject.IsActive = false;
        subject.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Subject deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
