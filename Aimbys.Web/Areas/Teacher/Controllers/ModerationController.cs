using Aimbys.Application.Moderation;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Moderation desk for teachers with the CanModerate permission.
/// Allows reviewing evaluator scores, approving, requesting changes,
/// or overriding scores with full audit trail.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class ModerationController : Controller
{
    private readonly IModerationService _moderationService;
    private readonly AppDbContext _db;

    public ModerationController(IModerationService moderationService, AppDbContext db)
    {
        _moderationService = moderationService;
        _db = db;
    }

    /// <summary>
    /// Moderator inbox: pending moderations assigned to the current teacher.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Forbid();

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (teacherProfile is null) return Forbid();

        var moderations = await _db.ModerationRecords
            .Where(m => m.ModeratorTeacherProfileId == teacherProfile.Id)
            .OrderByDescending(m => m.AssignedAtUtc)
            .ToListAsync(ct);

        return View(moderations);
    }

    /// <summary>
    /// Opens the moderation detail view: answer + evaluator scores + action panel.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Open(Guid moderationId, CancellationToken ct)
    {
        var context = await _moderationService.GetContextAsync(moderationId, ct);
        if (context is null) return NotFound();

        return View(context);
    }

    /// <summary>Approve the evaluation as-is.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid moderationId, CancellationToken ct)
    {
        var result = await _moderationService.ApproveAsync(moderationId, User, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Open), new { moderationId });
        }

        TempData["Success"] = "Moderation approved.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Send evaluation back for changes.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequireChanges(Guid moderationId, string comment, CancellationToken ct)
    {
        var result = await _moderationService.RequireChangesAsync(moderationId, User, comment, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Open), new { moderationId });
        }

        TempData["Success"] = "Changes requested.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Override the evaluated score with a new value.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Override(Guid moderationId, decimal newScore, decimal maxScore, string reason, CancellationToken ct)
    {
        var result = await _moderationService.OverrideAsync(moderationId, User, newScore, maxScore, reason, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Open), new { moderationId });
        }

        TempData["Success"] = "Score overridden.";
        return RedirectToAction(nameof(Index));
    }
}
