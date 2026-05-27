using Aimbys.Application.Authorization;
using Aimbys.Application.Questions;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin + "," + Roles.Teacher)]
public class QuestionReviewController : Controller
{
    private readonly AppDbContext _db;
    private readonly IQuestionLifecycleService _lifecycle;
    private readonly IInstituteScope _scope;
    private readonly UserManager<IdentityUser> _userManager;

    public QuestionReviewController(
        AppDbContext db,
        IQuestionLifecycleService lifecycle,
        IInstituteScope scope,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _lifecycle = lifecycle;
        _scope = scope;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        // Find teacher profile for current user.
        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (teacherProfile is null) return Forbid();

        var reviews = await _db.QuestionReviews
            .Where(r => r.ReviewerTeacherProfileId == teacherProfile.Id
                        && r.Verdict == ReviewVerdict.Pending)
            .OrderByDescending(r => r.AssignedAtUtc)
            .ToListAsync(ct);

        return View(reviews);
    }

    [HttpGet]
    public async Task<IActionResult> Open(Guid questionId, CancellationToken ct)
    {
        var question = await _db.Questions
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null) return NotFound();

        return View(question);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid questionId, string? comment, CancellationToken ct)
    {
        var result = await _lifecycle.ApproveAsync(questionId, User, comment, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Open), new { questionId });
        }

        TempData["Success"] = "Question approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid questionId, string comment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "A comment is required when rejecting.";
            return RedirectToAction(nameof(Open), new { questionId });
        }

        var result = await _lifecycle.RejectAsync(questionId, User, comment, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Open), new { questionId });
        }

        TempData["Success"] = "Question rejected.";
        return RedirectToAction(nameof(Index));
    }
}
