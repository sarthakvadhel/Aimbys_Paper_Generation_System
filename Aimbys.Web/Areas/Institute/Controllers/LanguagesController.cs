using Aimbys.Application.Authorization;
using Aimbys.Domain.Entities.Multilingual;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class LanguagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public LanguagesController(AppDbContext db, IInstituteScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var languages = await _db.Languages
            .Where(l => l.InstituteId == instituteId.Value)
            .OrderByDescending(l => l.IsDefault)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);

        return View(languages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Code and Name are required.";
            return RedirectToAction(nameof(Index));
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var language = new Language
        {
            InstituteId = instituteId.Value,
            Code = code.Trim(),
            Name = name.Trim()
        };

        _db.Languages.Add(language);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Language created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string code, string name, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var language = await _db.Languages
            .FirstOrDefaultAsync(l => l.Id == id && l.InstituteId == instituteId.Value, ct);
        if (language is null) return NotFound();

        language.Code = code.Trim();
        language.Name = name.Trim();
        language.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Language updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var languages = await _db.Languages
            .Where(l => l.InstituteId == instituteId.Value)
            .ToListAsync(ct);

        foreach (var lang in languages)
        {
            lang.IsDefault = lang.Id == id;
            lang.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Default language updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var language = await _db.Languages
            .FirstOrDefaultAsync(l => l.Id == id && l.InstituteId == instituteId.Value, ct);
        if (language is null) return NotFound();

        language.IsActive = !language.IsActive;
        language.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = language.IsActive ? "Language activated." : "Language deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
