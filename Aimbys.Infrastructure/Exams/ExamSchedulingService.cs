using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Exams;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Schedules an exam for a class batch against a paper version.
/// </summary>
public sealed class ExamSchedulingService : IExamSchedulingService
{
    private readonly AppDbContext _db;
    private readonly DomainEventCollector _events;
    private readonly IAuditWriter _audit;
    private readonly IInstituteScope _scope;
    private readonly ILogger<ExamSchedulingService> _logger;

    public ExamSchedulingService(
        AppDbContext db,
        DomainEventCollector events,
        IAuditWriter audit,
        IInstituteScope scope,
        ILogger<ExamSchedulingService> logger)
    {
        _db = db;
        _events = events;
        _audit = audit;
        _scope = scope;
        _logger = logger;
    }

    public async Task<ExamScheduleResult> ScheduleAsync(
        ExamScheduleRequest request,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId == null)
            return new ExamScheduleResult(false, "Institute context not resolved.");

        // Validate class batch exists within the institute
        var batchExists = await _db.ClassBatches
            .AnyAsync(cb => cb.Id == request.ClassBatchId && cb.InstituteId == instituteId.Value, ct);
        if (!batchExists)
            return new ExamScheduleResult(false, "Class batch not found in this institute.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var exam = new Exam
        {
            InstituteId = instituteId.Value,
            PaperVersionId = request.PaperVersionId,
            ClassBatchId = request.ClassBatchId,
            Title = request.Title,
            ScheduledAtUtc = request.ScheduledAtUtc,
            DurationMinutes = request.DurationMinutes,
            Status = ExamStatus.Scheduled,
            ScheduledByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Exams.Add(exam);

        _events.Enqueue(new ExamScheduledEvent
        {
            ExamId = exam.Id,
            PaperVersionId = exam.PaperVersionId,
            ClassBatchId = exam.ClassBatchId,
            ExamTitle = exam.Title,
            ScheduledAtUtc = exam.ScheduledAtUtc,
            ScheduledByUserId = actorUserId,
            InstituteId = instituteId.Value
        });

        await _audit.WriteAsync(
            "Exam.Scheduled",
            entityType: "Exam",
            entityId: exam.Id.ToString(),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new
            {
                exam.Title,
                exam.PaperVersionId,
                exam.ClassBatchId,
                exam.ScheduledAtUtc,
                exam.DurationMinutes
            }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Exam {ExamId} scheduled for class batch {ClassBatchId} at {ScheduledAtUtc}.",
            exam.Id, exam.ClassBatchId, exam.ScheduledAtUtc);

        return new ExamScheduleResult(true, ExamId: exam.Id);
    }

    public async Task<ExamGoLiveResult> GoLiveAsync(
        Guid examId,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new ExamGoLiveResult(false, "Institute context not resolved.");

        var exam = await _db.Exams
            .FirstOrDefaultAsync(e => e.Id == examId && e.InstituteId == instituteId.Value, ct);

        if (exam is null)
            return new ExamGoLiveResult(false, "Exam not found in this institute.");

        if (exam.Status != ExamStatus.Scheduled)
            return new ExamGoLiveResult(false, $"Only Scheduled exams can go live. Current status: {exam.Status}.");

        exam.Status = ExamStatus.Live;

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _audit.WriteAsync(
            "Exam.GoLive",
            entityType: "Exam",
            entityId: exam.Id.ToString(),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new
            {
                exam.Title,
                PreviousStatus = ExamStatus.Scheduled.ToString(),
                NewStatus = exam.Status.ToString(),
                exam.ScheduledAtUtc
            }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Exam {ExamId} moved to Live by {ActorUserId}.", exam.Id, actorUserId);

        return new ExamGoLiveResult(true, ExamId: exam.Id, Title: exam.Title);
    }
}
