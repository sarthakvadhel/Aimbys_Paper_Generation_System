using Aimbys.Application.Exams;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Records exam security events, manages heartbeats, and evaluates suspicion thresholds.
/// </summary>
public class ExamSecurityService : IExamSecurityService
{
    private readonly AppDbContext _db;

    public ExamSecurityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> RecordEventAsync(Guid attemptId, ExamEventType eventType, string? detailsJson, string studentUserId, CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);

        if (attempt is null)
            return false;

        var examEvent = new ExamEvent
        {
            AttemptId = attemptId,
            EventType = eventType,
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = detailsJson
        };

        _db.ExamEvents.Add(examEvent);
        await _db.SaveChangesAsync(ct);

        // Evaluate suspicion on security-relevant events
        if (eventType is ExamEventType.TabBlur or ExamEventType.FullscreenExit)
        {
            await EvaluateSuspicionAsync(attemptId, ct);
        }

        return true;
    }

    public async Task<bool> RecordHeartbeatAsync(Guid attemptId, string studentUserId, CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);

        if (attempt is null)
            return false;

        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.AttemptId == attemptId, ct);

        if (session is null)
        {
            session = new ExamSession
            {
                AttemptId = attemptId,
                StartedAtUtc = DateTime.UtcNow,
                LastHeartbeatAtUtc = DateTime.UtcNow
            };
            _db.ExamSessions.Add(session);
        }
        else
        {
            session.LastHeartbeatAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task EvaluateSuspicionAsync(Guid attemptId, CancellationToken ct = default)
    {
        var tabBlurCount = await _db.ExamEvents
            .CountAsync(e => e.AttemptId == attemptId && e.EventType == ExamEventType.TabBlur, ct);

        var fullscreenExitCount = await _db.ExamEvents
            .CountAsync(e => e.AttemptId == attemptId && e.EventType == ExamEventType.FullscreenExit, ct);

        if (tabBlurCount > 3 || fullscreenExitCount > 2)
        {
            var attempt = await _db.ExamAttempts.FindAsync(new object[] { attemptId }, ct);
            if (attempt is not null && !attempt.IsSuspicious)
            {
                attempt.IsSuspicious = true;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<IReadOnlyList<ExamEventSummary>> GetTimelineAsync(Guid attemptId, CancellationToken ct = default)
    {
        return await _db.ExamEvents
            .Where(e => e.AttemptId == attemptId)
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e => new ExamEventSummary(e.EventType, e.OccurredAtUtc, e.DetailsJson))
            .ToListAsync(ct);
    }
}
