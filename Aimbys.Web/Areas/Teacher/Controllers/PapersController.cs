using Aimbys.Application.Authorization;
using Aimbys.Application.Papers;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Teacher-facing paper authoring surface. Teachers create, edit,
/// preview, and submit papers for institute approval.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class PapersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IPaperAssemblyService _paperService;

    public PapersController(
        AppDbContext db,
        IInstituteScope scope,
        IPaperAssemblyService paperService)
    {
        _db = db;
        _scope = scope;
        _paperService = paperService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null) return Forbid();

        var papers = await _db.Papers
            .Where(p => p.AuthorTeacherProfileId == teacherProfile.Id)
            .Include(p => p.Versions)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(ct);

        return View(papers);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        var subjects = await _db.Subjects
            .Where(s => s.InstituteId == instituteId.Value && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        ViewBag.Subjects = subjects;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, Guid subjectId, int totalMarks, int durationMinutes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Title is required.";
            return RedirectToAction(nameof(Create));
        }

        var request = new PaperCreateRequest(title.Trim(), subjectId, totalMarks, durationMinutes);
        var result = await _paperService.CreateDraftAsync(request, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "Failed to create paper.";
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = "Paper draft created.";
        return RedirectToAction(nameof(Edit), new { id = result.PaperId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var paper = await _db.Papers
            .Include(p => p.Versions)
                .ThenInclude(v => v.Sections)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Questions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (paper is null) return NotFound();

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null || paper.AuthorTeacherProfileId != teacherProfile.Id)
            return Forbid();

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is not null)
        {
            var questions = await _db.Set<Aimbys.Domain.Entities.Questions.Question>()
                .Where(q => q.InstituteId == instituteId.Value
                    && q.Status == Aimbys.Domain.Enums.QuestionStatus.Approved)
                .Include(q => q.Versions.Where(v => v.IsCurrentVersion))
                .OrderBy(q => q.CreatedAtUtc)
                .ToListAsync(ct);
            ViewBag.AvailableQuestions = questions;
        }

        return View(paper);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string title, int totalMarks, int durationMinutes,
        [FromForm] List<Guid>? questionIds, CancellationToken ct)
    {
        var paper = await _db.Papers
            .Include(p => p.Versions)
                .ThenInclude(v => v.Sections)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Questions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (paper is null) return NotFound();

        // Update title / marks / duration directly on the current version
        var cv = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (cv is not null)
        {
            if (!string.IsNullOrWhiteSpace(title)) cv.Title = title.Trim();
            if (totalMarks > 0) cv.TotalMarks = totalMarks;
            if (durationMinutes > 0) cv.DurationMinutes = durationMinutes;
        }

        // Build sections + questions for SaveDraftAsync
        var sections = new List<PaperSectionInput>();
        var questions = new List<PaperQuestionInput>();

        if (questionIds?.Any() == true)
        {
            sections.Add(new PaperSectionInput("Section A", totalMarks, 1));

            // Resolve current version ids for each selected question
            var qVersionIds = await _db.Set<Aimbys.Domain.Entities.Questions.QuestionVersion>()
                .Where(v => questionIds.Contains(v.QuestionId) && v.IsCurrentVersion)
                .Select(v => new { v.QuestionId, v.Id })
                .ToListAsync(ct);

            var versionMap = qVersionIds.ToDictionary(x => x.QuestionId, x => x.Id);

            for (int i = 0; i < questionIds.Count; i++)
            {
                var qid = questionIds[i];
                if (versionMap.TryGetValue(qid, out var vid))
                    questions.Add(new PaperQuestionInput(0, qid, vid, i + 1, null));
            }
        }

        var request = new PaperSaveRequest(sections, questions);
        var result = await _paperService.SaveDraftAsync(id, request, User, ct);

        if (!result.Success)
            TempData["Error"] = result.Error ?? "Failed to save paper.";
        else
            TempData["Success"] = "Paper saved successfully.";

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await _paperService.SubmitForApprovalAsync(id, User, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "Failed to submit paper.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = "Paper submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        var paper = await _db.Papers
            .Include(p => p.Versions)
                .ThenInclude(v => v.Sections)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Questions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (paper is null) return NotFound();

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null || paper.AuthorTeacherProfileId != teacherProfile.Id)
            return Forbid();

        // Use the first version if CurrentVersionId doesn't match (data integrity guard)
        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId)
                          ?? paper.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (currentVersion != null && currentVersion.Questions.Any())
        {
            var questionIds = currentVersion.Questions.Select(q => q.QuestionId).ToList();

            // Load versions directly by QuestionId list
            var qVersions = await _db.Set<Aimbys.Domain.Entities.Questions.QuestionVersion>()
                .Where(v => v.IsCurrentVersion && questionIds.Contains(v.QuestionId))
                .Include(v => v.Options)
                .ToListAsync(ct);

            ViewBag.QuestionVersions = qVersions.ToDictionary(v => v.QuestionId);
        }
        else
        {
            ViewBag.QuestionVersions = new Dictionary<Guid, Aimbys.Domain.Entities.Questions.QuestionVersion>();
        }

        return View(paper);
    }
}
