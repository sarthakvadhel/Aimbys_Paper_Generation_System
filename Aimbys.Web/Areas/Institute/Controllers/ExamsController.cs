using Aimbys.Application.Exams;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Institute-admin exam management: calendar view and scheduling.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class ExamsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IExamSchedulingService _scheduling;

    public ExamsController(AppDbContext db, IExamSchedulingService scheduling)
    {
        _db = db;
        _scheduling = scheduling;
    }

    /// <summary>GET /Institute/Exams/Calendar — list of exams.</summary>
    [HttpGet]
    public async Task<IActionResult> Calendar(CancellationToken ct)
    {
        var exams = await _db.Exams
            .OrderByDescending(e => e.ScheduledAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return View(exams);
    }

    /// <summary>GET /Institute/Exams/Schedule — form.</summary>
    [HttpGet]
    public async Task<IActionResult> Schedule(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        var instituteId = profile?.InstituteId;

        ViewBag.ClassBatches = await _db.ClassBatches
            .Where(cb => instituteId == null || cb.InstituteId == instituteId)
            .OrderBy(cb => cb.Name)
            .ToListAsync(ct);

        // Load approved papers with their current version
        var papers = await _db.Papers
            .Where(p => (instituteId == null || p.InstituteId == instituteId)
                     && p.Status == Aimbys.Domain.Enums.PaperStatus.Approved)
            .Include(p => p.Versions)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(ct);

        ViewBag.Papers = papers;
        return View();
    }

    /// <summary>POST /Institute/Exams/Schedule — create exam.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(
        Guid paperVersionId,
        Guid classBatchId,
        string title,
        DateTime scheduledAtUtc,
        int durationMinutes,
        CancellationToken ct)
    {
        var request = new ExamScheduleRequest(
            paperVersionId,
            classBatchId,
            title,
            scheduledAtUtc,
            durationMinutes);

        var result = await _scheduling.ScheduleAsync(request, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Schedule));
        }

        TempData["Success"] = "Exam scheduled successfully.";
        return RedirectToAction(nameof(Calendar));
    }

    /// <summary>POST /Institute/Exams/GoLive — sets exam status to Live.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoLive(Guid id, CancellationToken ct)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exam is null) return NotFound();

        exam.Status = Aimbys.Domain.Enums.ExamStatus.Live;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = $"Exam '{exam.Title}' is now Live. Students can start it.";
        return RedirectToAction(nameof(Calendar));
    }
}
