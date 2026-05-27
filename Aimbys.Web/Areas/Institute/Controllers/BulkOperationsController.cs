using System.Globalization;
using System.Text;
using Aimbys.Application.Authorization;
using Aimbys.Application.Bulk;
using Aimbys.Infrastructure.Identity;
using Aimbys.Web.Areas.Institute.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
[EnableRateLimiting("bulk")]
public class BulkOperationsController : Controller
{
    private readonly IBulkOperationService _bulk;
    private readonly IInstituteScope _scope;

    public BulkOperationsController(IBulkOperationService bulk, IInstituteScope scope)
    {
        _bulk = bulk;
        _scope = scope;
    }

    // ===== Student Import =====
    [HttpGet]
    public IActionResult StudentImport() => View(new BulkResultViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentImport(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file.");
            return View(new BulkResultViewModel());
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File exceeds 10MB limit.");
            return View(new BulkResultViewModel());
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null)
            return Forbid();

        using var stream = file.OpenReadStream();
        var result = await _bulk.ImportStudentsAsync(instituteId.Value, stream, User, ct);

        return View(new BulkResultViewModel
        {
            HasResult = true,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Total = result.Total,
            Errors = result.Errors.ToList()
        });
    }

    [HttpGet]
    public IActionResult StudentImportTemplate()
    {
        var csv = "Email,DisplayName,AdmissionNumber,RollNumber,ClassBatchName\njohn@example.com,John Doe,ADM001,R001,Class 10-A\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "student-import-template.csv");
    }

    // ===== Teacher Assignment =====
    [HttpGet]
    public IActionResult TeacherAssignment() => View(new BulkResultViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeacherAssignment(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file.");
            return View(new BulkResultViewModel());
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File exceeds 10MB limit.");
            return View(new BulkResultViewModel());
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null)
            return Forbid();

        var assignments = await ParseTeacherAssignmentCsvAsync(file);
        var result = await _bulk.AssignTeachersAsync(instituteId.Value, assignments, User, ct);

        return View(new BulkResultViewModel
        {
            HasResult = true,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Total = result.Total,
            Errors = result.Errors.ToList()
        });
    }

    [HttpGet]
    public IActionResult TeacherAssignmentTemplate()
    {
        var csv = "TeacherProfileId,SubjectId,ClassBatchId\n00000000-0000-0000-0000-000000000001,,00000000-0000-0000-0000-000000000002\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "teacher-assignment-template.csv");
    }

    // ===== Exam Schedule =====
    [HttpGet]
    public IActionResult ExamSchedule() => View(new BulkResultViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExamSchedule(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file.");
            return View(new BulkResultViewModel());
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File exceeds 10MB limit.");
            return View(new BulkResultViewModel());
        }

        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null)
            return Forbid();

        var schedules = await ParseExamScheduleCsvAsync(file);
        var result = await _bulk.ScheduleExamsBulkAsync(instituteId.Value, schedules, User, ct);

        return View(new BulkResultViewModel
        {
            HasResult = true,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Total = result.Total,
            Errors = result.Errors.ToList()
        });
    }

    [HttpGet]
    public IActionResult ExamScheduleTemplate()
    {
        var csv = "PaperId,ClassBatchId,ScheduledAtUtc,DurationMinutes\n00000000-0000-0000-0000-000000000001,00000000-0000-0000-0000-000000000002,2026-06-01T09:00:00Z,120\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "exam-schedule-template.csv");
    }

    // ===== Result Publish =====
    [HttpGet]
    public IActionResult ResultPublish() => View(new BulkResultViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResultPublish(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file.");
            return View(new BulkResultViewModel());
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File exceeds 10MB limit.");
            return View(new BulkResultViewModel());
        }

        var examIds = await ParseGuidListCsvAsync(file, "ExamId");
        var result = await _bulk.PublishResultsBulkAsync(examIds, User, ct);

        return View(new BulkResultViewModel
        {
            HasResult = true,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Total = result.Total,
            Errors = result.Errors.ToList()
        });
    }

    [HttpGet]
    public IActionResult ResultPublishTemplate()
    {
        var csv = "ExamId\n00000000-0000-0000-0000-000000000001\n00000000-0000-0000-0000-000000000002\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "result-publish-template.csv");
    }

    // ===== Activation =====
    [HttpGet]
    public IActionResult Activation() => View(new BulkActivationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activation(IFormFile file, bool activate, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file.");
            return View(new BulkActivationViewModel());
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File exceeds 10MB limit.");
            return View(new BulkActivationViewModel());
        }

        var profileIds = await ParseGuidListCsvAsync(file, "ProfileId");
        var result = await _bulk.ActivateDeactivateBulkAsync(profileIds, activate, User, ct);

        return View(new BulkActivationViewModel
        {
            HasResult = true,
            Activate = activate,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Total = result.Total,
            Errors = result.Errors.ToList()
        });
    }

    [HttpGet]
    public IActionResult ActivationTemplate()
    {
        var csv = "ProfileId\n00000000-0000-0000-0000-000000000001\n00000000-0000-0000-0000-000000000002\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "activation-template.csv");
    }

    // ===== Private CSV parsers (just deserialization) =====

    private static async Task<IReadOnlyList<BulkTeacherAssignment>> ParseTeacherAssignmentCsvAsync(IFormFile file)
    {
        var results = new List<BulkTeacherAssignment>();
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        await reader.ReadLineAsync(); // skip header
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 1) continue;
            if (!Guid.TryParse(parts[0].Trim(), out var teacherId)) continue;
            Guid? subjectId = parts.Length > 1 && Guid.TryParse(parts[1].Trim(), out var s) ? s : null;
            Guid? classBatchId = parts.Length > 2 && Guid.TryParse(parts[2].Trim(), out var c) ? c : null;
            results.Add(new BulkTeacherAssignment(teacherId, subjectId, classBatchId));
        }
        return results;
    }

    private static async Task<IReadOnlyList<BulkExamSchedule>> ParseExamScheduleCsvAsync(IFormFile file)
    {
        var results = new List<BulkExamSchedule>();
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        await reader.ReadLineAsync(); // skip header
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 4) continue;
            if (!Guid.TryParse(parts[0].Trim(), out var paperId)) continue;
            if (!Guid.TryParse(parts[1].Trim(), out var classBatchId)) continue;
            if (!DateTime.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var scheduledAt)) continue;
            if (!int.TryParse(parts[3].Trim(), out var duration)) continue;
            results.Add(new BulkExamSchedule(paperId, classBatchId, scheduledAt, duration));
        }
        return results;
    }

    private static async Task<IReadOnlyList<Guid>> ParseGuidListCsvAsync(IFormFile file, string expectedHeader)
    {
        var results = new List<Guid>();
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        await reader.ReadLineAsync(); // skip header
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (Guid.TryParse(line.Trim(), out var id))
                results.Add(id);
        }
        return results;
    }
}
