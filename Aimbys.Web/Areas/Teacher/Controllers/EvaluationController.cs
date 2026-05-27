using System.Security.Claims;
using Aimbys.Application.Evaluation;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Evaluator inbox and scoring surface. Teachers with CanEvaluate permission
/// see their assigned evaluations, open answer + rubric, save draft scores,
/// and submit for moderation.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class EvaluationController : Controller
{
    private readonly IEvaluationService _evaluationService;
    private readonly AppDbContext _db;

    public EvaluationController(IEvaluationService evaluationService, AppDbContext db)
    {
        _evaluationService = evaluationService;
        _db = db;
    }

    /// <summary>Evaluator inbox: evaluations assigned to the current teacher.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (teacherProfile is null) return Forbid();

        var evaluations = await _db.Evaluations
            .Where(e => e.EvaluatorTeacherProfileId == teacherProfile.Id)
            .OrderBy(e => e.AssignedAtUtc)
            .ToListAsync(ct);

        return View(evaluations);
    }

    /// <summary>Open an evaluation: shows the student answer and rubric scoring form.</summary>
    [HttpGet]
    public async Task<IActionResult> Open(Guid evaluationId, CancellationToken ct)
    {
        var context = await _evaluationService.GetScoringContextAsync(evaluationId, ct);
        if (context is null) return NotFound();

        return View(context);
    }

    /// <summary>Save a draft score for one rubric criterion (AJAX).</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveDraft(
        [FromBody] SaveDraftInput input, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var ok = await _evaluationService.SaveDraftScoreAsync(
            input.EvaluationId, input.CriterionIndex, input.Points, userId, ct);

        return ok ? Ok(new { success = true }) : BadRequest(new { error = "Evaluation not found." });
    }

    /// <summary>Save feedback draft (AJAX).</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveFeedback(
        [FromBody] SaveFeedbackInput input, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var ok = await _evaluationService.SaveFeedbackDraftAsync(
            input.EvaluationId, input.Feedback, userId, ct);

        return ok ? Ok(new { success = true }) : BadRequest(new { error = "Evaluation not found." });
    }

    /// <summary>Submit the evaluation for moderation.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid evaluationId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _evaluationService.SubmitAsync(evaluationId, userId, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Open), new { evaluationId });
        }

        TempData["Success"] = "Evaluation submitted for moderation.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed class SaveDraftInput
{
    public Guid EvaluationId { get; set; }
    public int CriterionIndex { get; set; }
    public decimal Points { get; set; }
}

public sealed class SaveFeedbackInput
{
    public Guid EvaluationId { get; set; }
    public string Feedback { get; set; } = string.Empty;
}
