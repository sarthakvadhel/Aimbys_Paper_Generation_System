using System.Security.Claims;
using System.Text;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Questions;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.ViewModels.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin + "," + Roles.Teacher)]
public class QuestionsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IQuestionLifecycleService _lifecycle;
    private readonly IInstituteScope _scope;
    private readonly IAuditWriter _audit;

    public QuestionsController(
        AppDbContext db,
        IQuestionLifecycleService lifecycle,
        IInstituteScope scope,
        IAuditWriter audit)
    {
        _db = db;
        _lifecycle = lifecycle;
        _scope = scope;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (profile is null)
            return View(new QuestionIndexViewModel());

        var questions = await _db.Set<Question>()
            .Where(q => q.InstituteId == profile.InstituteId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Select(q => new QuestionRowViewModel
            {
                Id = q.Id,
                Type = q.Type,
                Status = q.Status,
                SubjectName = _db.Subjects
                    .Where(s => s.Id == q.SubjectId)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? "—",
                Difficulty = _db.Set<QuestionVersion>()
                    .Where(v => v.QuestionId == q.Id && v.IsCurrentVersion)
                    .Select(v => v.Difficulty)
                    .FirstOrDefault(),
                Marks = _db.Set<QuestionVersion>()
                    .Where(v => v.QuestionId == q.Id && v.IsCurrentVersion)
                    .Select(v => v.Marks)
                    .FirstOrDefault(),
                CreatedAtUtc = q.CreatedAtUtc,
                BodyPreview = _db.Set<QuestionVersion>()
                    .Where(v => v.QuestionId == q.Id && v.IsCurrentVersion)
                    .Select(v => v.BodyHtml.Substring(0, 100))
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(ct);

        return View(new QuestionIndexViewModel { Questions = questions });
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,SubjectCode,BodyHtml,Difficulty,BloomLevel,Marks,OptionA,OptionB,OptionC,OptionD,CorrectAnswer");
        sb.AppendLine("MCQ,MATH101,What is 2+2?,Easy,Remember,1,3,4,5,6,B");
        sb.AppendLine("Descriptive,ENG101,Explain photosynthesis.,Medium,Understand,5,,,,, ");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "questions-template.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        // Show available subjects so user knows what to put in SubjectCode column
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (profile is not null)
        {
            var subjects = await _db.Subjects
                .Where(s => s.InstituteId == profile.InstituteId && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(ct);
            ViewBag.Subjects = subjects;
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (teacherProfile is null)
        {
            TempData["Error"] = "No profile found for your account.";
            return RedirectToAction(nameof(Import));
        }

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            return RedirectToAction(nameof(Import));
        }

        var instituteId = teacherProfile.InstituteId;

        var subjects = await _db.Subjects
            .Where(s => s.InstituteId == instituteId && s.IsActive)
            .ToListAsync(ct);

        if (!subjects.Any())
        {
            TempData["Error"] = "No active subjects found. Go to Settings → Subjects and create at least one subject first.";
            return RedirectToAction(nameof(Import));
        }

        // Build lookup by both Code and Name
        var subjectLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in subjects)
        {
            if (!string.IsNullOrWhiteSpace(s.Code))
                subjectLookup.TryAdd(s.Code.Trim(), s.Id);
            subjectLookup.TryAdd(s.Name.Trim(), s.Id);
        }

        var errors = new List<string>();
        int imported = 0;
        int rowNum = 1;

        using var reader = new StreamReader(file.OpenReadStream());
        var header = await reader.ReadLineAsync(ct); // skip header
        if (header is null)
        {
            TempData["Error"] = "CSV file is empty.";
            return RedirectToAction(nameof(Import));
        }

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitCsvLine(line);
            if (cols.Count < 6)
            {
                errors.Add($"Row {rowNum}: only {cols.Count} columns, need 6.");
                continue;
            }

            var typeStr    = cols[0].Trim();
            var subjectKey = cols[1].Trim();
            var bodyHtml   = cols[2].Trim();
            var diffStr    = cols[3].Trim();
            var bloomStr   = cols[4].Trim();
            var marksStr   = cols[5].Trim();

            if (string.IsNullOrWhiteSpace(bodyHtml))
            {
                errors.Add($"Row {rowNum}: question text (BodyHtml) is empty.");
                continue;
            }

            if (!Enum.TryParse<QuestionType>(typeStr, true, out var qType))
            {
                errors.Add($"Row {rowNum}: unknown Type '{typeStr}'. Use MCQ or Descriptive.");
                continue;
            }

            if (!subjectLookup.TryGetValue(subjectKey, out var subjectId))
            {
                var available = string.Join(", ", subjects.Select(s => s.Code ?? s.Name));
                errors.Add($"Row {rowNum}: subject '{subjectKey}' not found. Available: {available}");
                continue;
            }

            if (!Enum.TryParse<DifficultyLevel>(diffStr, true, out var difficulty))
                difficulty = DifficultyLevel.Medium;

            if (!Enum.TryParse<BloomLevel>(bloomStr, true, out var bloom))
                bloom = BloomLevel.Remember;

            if (!decimal.TryParse(marksStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var marks) || marks <= 0)
                marks = 1;

            var question = new Question
            {
                InstituteId = instituteId,
                SubjectId = subjectId,
                AuthorTeacherProfileId = teacherProfile.Id,
                AuthorUserId = userId,
                Type = qType,
                Status = QuestionStatus.Draft
            };

            var version = new QuestionVersion
            {
                QuestionId = question.Id,
                VersionNumber = 1,
                BodyHtml = bodyHtml,
                Difficulty = difficulty,
                BloomLevel = bloom,
                Marks = marks,
                AuthorUserId = userId,
                IsCurrentVersion = true
            };

            if (qType == QuestionType.MCQ && cols.Count >= 11)
            {
                var labels = new[] { "A", "B", "C", "D" };
                var correct = cols[10].Trim().ToUpperInvariant();
                for (int i = 0; i < 4; i++)
                {
                    var txt = cols[6 + i].Trim();
                    if (string.IsNullOrWhiteSpace(txt)) continue;
                    version.Options.Add(new QuestionOption
                    {
                        VersionId = version.Id,
                        Label = labels[i],
                        Text = txt,
                        IsCorrect = correct == labels[i],
                        SortOrder = i + 1
                    });
                }
            }

            question.CurrentVersionId = version.Id;
            question.Versions.Add(version);
            _db.Set<Question>().Add(question);
            imported++;
        }

        if (imported > 0)
            await _db.SaveChangesAsync(ct);

        if (errors.Any())
            TempData["Error"] = $"{imported} imported, {errors.Count} skipped — {string.Join(" | ", errors)}";
        else
            TempData["Success"] = $"{imported} question(s) imported. They appear as Draft status in the Question Bank.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.InstituteAdmin)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (profile is null) return Forbid();

        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == id && q.InstituteId == profile.InstituteId, ct);
        if (question is null) return NotFound();

        question.Status = QuestionStatus.Approved;
        question.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Question approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.InstituteAdmin)]
    public async Task<IActionResult> ApproveAll(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (profile is null) return Forbid();

        var drafts = await _db.Set<Question>()
            .Where(q => q.InstituteId == profile.InstituteId && q.Status == QuestionStatus.Draft)
            .ToListAsync(ct);

        foreach (var q in drafts)
        {
            q.Status = QuestionStatus.Approved;
            q.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = $"{drafts.Count} question(s) approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignReviewer(Guid questionId, Guid reviewerProfileId, CancellationToken ct)
    {
        var result = await _lifecycle.AssignReviewerAsync(questionId, reviewerProfileId, User, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Reviewer assigned.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (profile is null) return Forbid();

        var questions = await _db.Set<Question>()
            .Where(q => q.InstituteId == profile.InstituteId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Select(q => new
            {
                q.Id, q.Type, q.Status, q.CreatedAtUtc,
                SubjectName = _db.Subjects.Where(s => s.Id == q.SubjectId).Select(s => s.Name).FirstOrDefault() ?? ""
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Type,Status,Subject,CreatedAtUtc");
        foreach (var q in questions)
            sb.AppendLine($"\"{q.Id}\",\"{q.Type}\",\"{q.Status}\",\"{q.SubjectName.Replace("\"", "\"\"")}\",\"{q.CreatedAtUtc:O}\"");

        await _audit.WriteAsync("Questions.Exported", "Question", profile.InstituteId.ToString(),
            userId, $"{{\"count\":{questions.Count}}}", AuditSeverity.Information, ct);
        await _db.SaveChangesAsync(ct);

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"questions-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        foreach (var c in line)
        {
            if (inQuotes) { if (c == '"') inQuotes = false; else sb.Append(c); }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
