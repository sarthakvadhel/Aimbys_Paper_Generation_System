using System.Security.Claims;
using Aimbys.Application.Exams;
using Aimbys.Application.Storage;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Student.Controllers;

/// <summary>
/// Student exam-taking surface: list exams, start attempts, take
/// exam (fullscreen), autosave answers, flag, and submit.
/// </summary>
[Area("Student")]
[Authorize(Roles = Roles.Student)]
public class ExamsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IExamRuntimeService _runtime;
    private readonly IFileStorageService _fileStorage;

    public ExamsController(AppDbContext db, IExamRuntimeService runtime, IFileStorageService fileStorage)
    {
        _db = db;
        _runtime = runtime;
        _fileStorage = fileStorage;
    }

    /// <summary>GET /Student/Exams — upcoming + completed exams.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var studentProfile = await _db.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId, ct);

        if (studentProfile == null)
            return View(new StudentExamsViewModel());

        var exams = await _db.Exams
            .Where(e => e.ClassBatchId == studentProfile.ClassBatchId)
            .OrderByDescending(e => e.ScheduledAtUtc)
            .ToListAsync(ct);

        var attempts = await _db.ExamAttempts
            .Where(a => a.StudentProfileId == studentProfile.Id)
            .ToListAsync(ct);

        var submittedExamIds = attempts
            .Where(a => a.Status == AttemptStatus.Submitted
                     || a.Status == AttemptStatus.Evaluated
                     || a.Status == AttemptStatus.Published)
            .Select(a => a.ExamId)
            .ToHashSet();

        var vm = new StudentExamsViewModel
        {
            StudentProfileId = studentProfile.Id,
            Upcoming = exams
                .Where(e => (e.Status == ExamStatus.Scheduled || e.Status == ExamStatus.Live)
                            && !submittedExamIds.Contains(e.Id))
                .ToList(),
            Completed = exams
                .Where(e => e.Status == ExamStatus.Completed
                            || e.Status == ExamStatus.ResultsPublished
                            || submittedExamIds.Contains(e.Id))
                .ToList(),
            Attempts = attempts
        };

        return View(vm);
    }

    /// <summary>POST /Student/Exams/Start — starts an attempt.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid examId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var studentProfile = await _db.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId, ct);

        if (studentProfile == null)
        {
            TempData["Error"] = "Student profile not found.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _runtime.StartAttemptAsync(examId, studentProfile.Id, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Take), new { attemptId = result.AttemptId });
    }

    /// <summary>GET /Student/Exams/Take?attemptId=... — fullscreen exam view.</summary>
    [HttpGet]
    public async Task<IActionResult> Take(Guid attemptId, CancellationToken ct)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt?.Exam == null)
            return NotFound();

        // Ownership check
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var studentProfile = await _db.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId, ct);
        if (studentProfile is null || attempt.StudentProfileId != studentProfile.Id)
            return Forbid();

        // Load question bodies for all answers
        var questionIds = attempt.Answers.Select(a => a.QuestionId).ToList();
        var qVersions = await _db.Set<QuestionVersion>()
            .Where(v => questionIds.Contains(v.QuestionId) && v.IsCurrentVersion)
            .Include(v => v.Options)
            .ToListAsync(ct);
        ViewBag.QuestionVersions = qVersions.ToDictionary(v => v.QuestionId);

        var remainingSeconds = 0;
        if (attempt.StartedAtUtc.HasValue)
        {
            var deadline = attempt.StartedAtUtc.Value.AddMinutes(attempt.Exam.DurationMinutes);
            remainingSeconds = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalSeconds);
        }

        ViewBag.RemainingSeconds = remainingSeconds;
        return View(attempt);
    }

    /// <summary>
    /// POST /Student/Exams/SaveAnswer — autosave from JS.
    /// Uses [IgnoreAntiforgeryToken] — real CSRF protection via header
    /// token is deferred; the cookie-based auth + SameSite policy covers
    /// the immediate threat model.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerInput input, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _runtime.SaveAnswerAsync(input.AttemptId, input.QuestionId, input.AnswerJson, userId, ct);

        if (!result.Success && result.Error == "timer_expired")
            return Conflict(new { error = "timer_expired" });

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// POST /Student/Exams/Flag — toggle flag from JS.
    /// Uses [IgnoreAntiforgeryToken] for same reason as SaveAnswer.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Flag([FromBody] FlagInput input, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _runtime.FlagQuestionAsync(input.AttemptId, input.QuestionId, input.Flagged, userId, ct);
        return Ok(new { success = true });
    }

    /// <summary>POST /Student/Exams/Submit — submits the attempt.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid attemptId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _runtime.SubmitAsync(attemptId, userId, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Take), new { attemptId });
        }

        TempData["Success"] = $"Exam submitted. Auto-score: {result.TotalAutoScore}";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>POST /Student/Exams/UploadAnswer — file upload for FileUpload-type questions.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAnswer(Guid attemptId, Guid questionId, IFormFile file, CancellationToken ct)
    {
        // Validate attempt belongs to current student
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var attempt = await _db.ExamAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt == null)
            return NotFound();

        var studentProfile = await _db.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId, ct);
        if (studentProfile == null || attempt.StudentProfileId != studentProfile.Id)
            return Forbid();

        // Get question version to check allowed MIMEs and max size
        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);
        if (question == null || question.Type != QuestionType.FileUpload)
            return BadRequest(new { error = "Question is not a file upload type." });

        var currentVersion = await _db.Set<QuestionVersion>()
            .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.IsCurrentVersion, ct);

        var mimeAllowList = currentVersion?.AllowedMimeTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { "application/pdf", "image/jpeg", "image/png" };
        var maxBytes = currentVersion?.MaxFileSizeBytes ?? 10 * 1024 * 1024L;

        try
        {
            var result = await _fileStorage.SaveAsync(
                FileArea.Answers,
                $"Attempt:{attemptId}:Question:{questionId}",
                file,
                mimeAllowList,
                maxBytes,
                studentProfile.InstituteId,
                User,
                ct);

            // Update or create the answer record
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId);
            if (answer == null)
            {
                answer = new ExamAttemptAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = questionId,
                    QuestionVersionId = currentVersion?.Id ?? Guid.Empty,
                    FileAssetId = result.Asset.Id,
                    LastSavedAtUtc = DateTime.UtcNow
                };
                _db.Set<ExamAttemptAnswer>().Add(answer);
            }
            else
            {
                answer.FileAssetId = result.Asset.Id;
                answer.LastSavedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { success = true, fileToken = result.Token });
        }
        catch (FileStorageException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// ----- View models and input DTOs (co-located for simplicity) -----

public sealed class StudentExamsViewModel
{
    public Guid StudentProfileId { get; set; }
    public List<Exam> Upcoming { get; set; } = new();
    public List<Exam> Completed { get; set; } = new();
    public List<ExamAttempt> Attempts { get; set; } = new();
}

public sealed class SaveAnswerInput
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string? AnswerJson { get; set; }
}

public sealed class FlagInput
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public bool Flagged { get; set; }
}
