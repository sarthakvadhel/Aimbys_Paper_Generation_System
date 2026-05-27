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
public class DepartmentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IOrgTreeService _orgTree;

    public DepartmentsController(AppDbContext db, IInstituteScope scope, IOrgTreeService orgTree)
    {
        _db = db;
        _scope = scope;
        _orgTree = orgTree;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var departments = await _db.Departments
            .Where(d => d.InstituteId == instituteId.Value)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        return View(departments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var department = new Domain.Entities.Department
        {
            InstituteId = instituteId.Value,
            Name = name.Trim(),
            Code = code?.Trim()
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Department created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string? code, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.InstituteId == instituteId.Value, ct);
        if (dept is null) return NotFound();

        dept.Name = name.Trim();
        dept.Code = code?.Trim();
        dept.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Department updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var (success, error) = await _orgTree.DeactivateDepartmentAsync(id, User, ct);
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Department deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
