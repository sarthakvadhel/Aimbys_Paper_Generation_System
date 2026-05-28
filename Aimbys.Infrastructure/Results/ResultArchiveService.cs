using System.Text;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Results;
using Aimbys.Application.Storage;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Results;

/// <summary>
/// Builds an immutable JSON snapshot of an exam's published results plus
/// every contributing FinalPublishedScore, persists the bytes via
/// <see cref="ILocalFileStorageService.SaveBytesAsync"/>, and writes a
/// <see cref="ResultArchive"/> row that pins the snapshot to the exam.
///
/// <para>
/// The snapshot is intentionally self-contained: it does not link out
/// to any other tables. Long after the source data has been
/// soft-deleted or migrated, the JSON file plus the
/// <c>FileAsset.Sha256</c> on the <c>ResultArchive</c> row are enough
/// to satisfy a regulator's "show me the results as published on
/// {date}" request.
/// </para>
/// </summary>
public sealed class ResultArchiveService : IResultArchiveService
{
    private readonly AppDbContext _db;
    private readonly ILocalFileStorageService _storage;
    private readonly IAuditWriter _audit;
    private readonly ILogger<ResultArchiveService> _logger;

    public ResultArchiveService(
        AppDbContext db,
        ILocalFileStorageService storage,
        IAuditWriter audit,
        ILogger<ResultArchiveService> logger)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ResultArchiveResult> CreateSnapshotAsync(
        Guid examId, string actorUserId, CancellationToken ct = default)
    {
        var exam = await _db.Exams.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId, ct);

        if (exam is null)
            return new ResultArchiveResult(false, "Exam not found.", null, null);

        // Pull every published attempt + result + score for this exam in
        // one go. Quantities are bounded by class-batch size; pulling
        // into memory keeps the snapshot deterministic.
        var attempts = await _db.ExamAttempts.AsNoTracking()
            .Where(a => a.ExamId == examId)
            .Select(a => new
            {
                a.Id,
                a.StudentProfileId,
                a.StudentUserId,
                a.StartedAtUtc,
                a.SubmittedAtUtc,
                a.Status,
                a.TotalAutoScore
            })
            .ToListAsync(ct);

        if (attempts.Count == 0)
            return new ResultArchiveResult(false, "No attempts found for this exam.", null, null);

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var results = await _db.Results.AsNoTracking()
            .Where(r => attemptIds.Contains(r.ExamAttemptId) && r.IsPublished)
            .ToListAsync(ct);

        if (results.Count == 0)
            return new ResultArchiveResult(false, "No published results to archive.", null, null);

        var answerIds = await _db.ExamAttemptAnswers.AsNoTracking()
            .Where(ans => attemptIds.Contains(ans.AttemptId))
            .Select(ans => new { ans.Id, ans.AttemptId, ans.QuestionId })
            .ToListAsync(ct);

        var allAnswerIds = answerIds.Select(a => a.Id).ToList();

        var scores = await _db.FinalPublishedScores.AsNoTracking()
            .Where(fps => allAnswerIds.Contains(fps.ExamAttemptAnswerId))
            .ToListAsync(ct);

        // Assemble the snapshot. Anonymous types are deliberate &mdash;
        // we don't want this JSON to drift if the entity surface changes.
        var snapshot = new
        {
            schemaVersion = 1,
            examId = exam.Id,
            examTitle = exam.Title,
            instituteId = exam.InstituteId,
            classBatchId = exam.ClassBatchId,
            paperVersionId = exam.PaperVersionId,
            scheduledAtUtc = exam.ScheduledAtUtc,
            durationMinutes = exam.DurationMinutes,
            archivedAtUtc = DateTime.UtcNow,
            archivedByUserId = actorUserId,
            studentCount = results.Count,
            attempts = attempts.Select(a =>
            {
                var result = results.FirstOrDefault(r => r.ExamAttemptId == a.Id);
                var attemptAnswers = answerIds.Where(an => an.AttemptId == a.Id).ToList();
                return new
                {
                    attemptId = a.Id,
                    studentProfileId = a.StudentProfileId,
                    studentUserId = a.StudentUserId,
                    startedAtUtc = a.StartedAtUtc,
                    submittedAtUtc = a.SubmittedAtUtc,
                    status = a.Status.ToString(),
                    autoScore = a.TotalAutoScore,
                    result = result == null ? null : new
                    {
                        totalScore = result.TotalScore,
                        maxScore = result.MaxScore,
                        percentage = result.Percentage,
                        grade = result.Grade,
                        rank = result.RankInBatch,
                        percentile = result.Percentile,
                        publishedAtUtc = result.PublishedAtUtc,
                        publishedByUserId = result.PublishedByUserId
                    },
                    answers = attemptAnswers.Select(an => new
                    {
                        answerId = an.Id,
                        questionId = an.QuestionId,
                        scores = scores
                            .Where(s => s.ExamAttemptAnswerId == an.Id)
                            .Select(s => new
                            {
                                points = s.PointsAwarded,
                                max = s.MaxPoints,
                                source = s.Source.ToString(),
                                computedAtUtc = s.ComputedAtUtc
                            })
                            .ToList()
                    }).ToList()
                };
            }).ToList()
        };

        var json = JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);

        var fileName = $"results-{exam.Id:N}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

        var saveResult = await _storage.SaveBytesAsync(
            area: FileArea.Reports,
            ownerKey: $"Exam:{exam.Id}",
            fileName: fileName,
            contentType: "application/json",
            bytes: bytes,
            instituteId: exam.InstituteId,
            actorUserId: actorUserId,
            cancellationToken: ct);

        var archive = new ResultArchive
        {
            ExamId = exam.Id,
            ArchiveType = ArchiveType.JsonSnapshot,
            FileAssetId = saveResult.Asset.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<ResultArchive>().Add(archive);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            "Result.Archived",
            "Exam",
            exam.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new
            {
                archiveId = archive.Id,
                fileToken = saveResult.Token,
                bytes = bytes.LongLength,
                students = results.Count
            }),
            cancellationToken: ct);

        _logger.LogInformation(
            "Result archive snapshot created. Exam={ExamId} Archive={ArchiveId} Bytes={Bytes}",
            exam.Id, archive.Id, bytes.LongLength);

        return new ResultArchiveResult(true, null, archive.Id, saveResult.Token);
    }
}
