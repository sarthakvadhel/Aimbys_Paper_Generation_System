using Aimbys.Application.Authorization;
using Aimbys.Application.Results;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Institute-level result-publication inbox. Lists every exam in the
/// tenant with its publication status (ready / blocked / partially or
/// fully published) and routes the publish action through
/// <see cref="IResultPublicationService.PublishAsync"/>.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class ResultsController : Controller
{
    private readonly IResultPublicationService _publication;
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public ResultsController(
        IResultPublicationService publication,
        AppDbContext db,
        IInstituteScope scope)
    {
        _publication = publication;
        _db = db;
        _scope = scope;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        // Pull exams that have at least one submitted attempt (anything to
        // publish at all). Ordering: most recent activity first.
        var exams = await _db.Exams
            .AsNoTracking()
            .Where(e => e.InstituteId == instituteId.Value)
            .OrderByDescending(e => e.ScheduledAtUtc)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.ScheduledAtUtc,
                e.Status
            })
            .ToListAsync(ct);

        if (exams.Count == 0)
        {
            return View(Array.Empty<ExamPublicationRow>());
        }

        var examIds = exams.Select(e => e.Id).ToList();

        // Aggregate counts per exam with a single roundtrip per metric.
        var attemptCountsRaw = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.Status != AttemptStatus.InProgress)
            .GroupBy(a => a.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var publishedCountsRaw = await (
            from r in _db.Results.AsNoTracking()
            where r.IsPublished
            join a in _db.ExamAttempts.AsNoTracking() on r.ExamAttemptId equals a.Id
            where examIds.Contains(a.ExamId)
            group r by a.ExamId into g
            select new { ExamId = g.Key, Count = g.Count() }
        ).ToListAsync(ct);

        var archiveCountsRaw = await _db.Set<ResultArchive>()
            .AsNoTracking()
            .Where(ra => examIds.Contains(ra.ExamId))
            .GroupBy(ra => ra.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var attemptCounts = attemptCountsRaw.ToDictionary(x => x.ExamId, x => x.Count);
        var publishedCounts = publishedCountsRaw.ToDictionary(x => x.ExamId, x => x.Count);
        var archiveCounts = archiveCountsRaw.ToDictionary(x => x.ExamId, x => x.Count);

        // CanPublish blocking-reason check: per-exam, sequential. The list
        // is bounded by exams-per-institute (small), so the cost is fine
        // for V1; if it gets noticeable we cache or batch later.
        var rows = new List<ExamPublicationRow>(exams.Count);
        foreach (var e in exams)
        {
            var totalAttempts = attemptCounts.TryGetValue(e.Id, out var ta) ? ta : 0;
            var publishedAttempts = publishedCounts.TryGetValue(e.Id, out var pa) ? pa : 0;
            var archives = archiveCounts.TryGetValue(e.Id, out var arc) ? arc : 0;

            string? blocking = null;
            if (totalAttempts > 0 && publishedAttempts < totalAttempts)
            {
                var (canPublish, reason) = await _publication.CanPublishAsync(e.Id, ct);
                if (!canPublish) blocking = reason;
            }

            rows.Add(new ExamPublicationRow(
                ExamId: e.Id,
                Title: e.Title,
                ScheduledAtUtc: e.ScheduledAtUtc,
                ExamStatus: e.Status,
                TotalAttempts: totalAttempts,
                PublishedAttempts: publishedAttempts,
                ArchiveCount: archives,
                BlockingReason: blocking));
        }

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid examId, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return Forbid();

        // Tenancy: verify the exam belongs to the current institute before
        // publishing &mdash; the service is tenant-agnostic by design.
        var examInstituteId = await _db.Exams.AsNoTracking()
            .Where(e => e.Id == examId)
            .Select(e => (Guid?)e.InstituteId)
            .FirstOrDefaultAsync(ct);

        if (examInstituteId is null || examInstituteId.Value != instituteId.Value)
            return Forbid();

        var result = await _publication.PublishAsync(examId, User, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Results published for {result.StudentsPublished} student(s).";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Archives() => View();
}

/// <summary>Row model for the institute publish inbox.</summary>
public sealed record ExamPublicationRow(
    Guid ExamId,
    string Title,
    DateTime ScheduledAtUtc,
    ExamStatus ExamStatus,
    int TotalAttempts,
    int PublishedAttempts,
    int ArchiveCount,
    string? BlockingReason);
