using Aimbys.Application.Exams;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Records exam security events, manages heartbeats, and evaluates
/// suspicion thresholds. Every mutating method enforces ownership at the
/// service boundary; the controller cannot bypass it.
/// </summary>
public class ExamSecurityService : IExamSecurityService
{
    /// <summary>
    /// Soft suspicion threshold: marks <c>ExamAttempt.IsSuspicious</c>.
    /// </summary>
    private const int SoftTabBlurThreshold = 3;
    private const int SoftFullscreenExitThreshold = 2;

    /// <summary>
    /// Critical suspicion threshold: fires
    /// <see cref="IExamRuntimeService.AutoSubmitAsync"/> when the
    /// per-exam <c>ExamSecurityProfile.AutoSubmitOnTimeout</c> flag is
    /// enabled (extended in Slice D to also cover the suspicion path).
    /// </summary>
    private const int CriticalTabBlurThreshold = 6;
    private const int CriticalFullscreenExitThreshold = 4;

    private readonly AppDbContext _db;
    private readonly IExamRuntimeService _runtime;
    private readonly ILogger<ExamSecurityService> _logger;

    public ExamSecurityService(
        AppDbContext db,
        IExamRuntimeService runtime,
        ILogger<ExamSecurityService> logger)
    {
        _db = db;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<bool> RecordEventAsync(
        Guid attemptId,
        ExamEventType eventType,
        string? detailsJson,
        string studentUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(studentUserId)) return false;

        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);
        if (attempt is null) return false;

        // Don't record post-submit noise; the timeline closes when the
        // attempt is submitted.
        if (attempt.Status != AttemptStatus.InProgress) return false;

        _db.ExamEvents.Add(new ExamEvent
        {
            AttemptId = attemptId,
            EventType = eventType,
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = detailsJson
        });
        await _db.SaveChangesAsync(ct);

        // Re-evaluate suspicion on every security-relevant event so the
        // critical-threshold path can fire as soon as the limit is hit.
        if (eventType is ExamEventType.TabBlur
                       or ExamEventType.FullscreenExit
                       or ExamEventType.PasteAttempt
                       or ExamEventType.KeyboardShortcut)
        {
            await EvaluateSuspicionAsync(attemptId, ct);
        }

        return true;
    }

    public async Task<bool> RecordHeartbeatAsync(
        Guid attemptId,
        string studentUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(studentUserId)) return false;

        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);
        if (attempt is null || attempt.Status != AttemptStatus.InProgress) return false;

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

    public async Task<ReconnectInfo?> ReconnectAsync(
        Guid attemptId,
        string studentUserId,
        string? deviceFingerprint,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(studentUserId)) return null;

        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);
        if (attempt is null || attempt.Exam is null) return null;
        if (attempt.Status != AttemptStatus.InProgress) return null;

        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.AttemptId == attemptId, ct);

        var sessionWasNew = session is null;
        var wasInactive = false;

        if (session is null)
        {
            session = new ExamSession
            {
                AttemptId = attemptId,
                DeviceFingerprint = deviceFingerprint,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                StartedAtUtc = DateTime.UtcNow,
                LastHeartbeatAtUtc = DateTime.UtcNow
            };
            _db.ExamSessions.Add(session);
        }
        else
        {
            // If the last heartbeat is older than the security profile's
            // tolerance, the session was effectively dead &mdash; record it
            // so the proctor sees the gap on the timeline.
            var tolerance = await GetMaxConnectionLossSecondsAsync(attempt.ExamId, ct);
            wasInactive = (DateTime.UtcNow - session.LastHeartbeatAtUtc).TotalSeconds > tolerance;

            session.DeviceFingerprint = deviceFingerprint;
            session.UserAgent = userAgent;
            session.IpAddress = ipAddress;
            session.LastHeartbeatAtUtc = DateTime.UtcNow;
        }

        if (wasInactive)
        {
            _db.ExamEvents.Add(new ExamEvent
            {
                AttemptId = attemptId,
                EventType = ExamEventType.ConnectionLost,
                OccurredAtUtc = DateTime.UtcNow
            });
        }

        _db.ExamEvents.Add(new ExamEvent
        {
            AttemptId = attemptId,
            EventType = ExamEventType.ConnectionRestored,
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = string.IsNullOrEmpty(deviceFingerprint)
                ? null
                : $"{{\"fingerprint\":\"{deviceFingerprint.Replace("\"", "\\\"")}\"}}"
        });

        await _db.SaveChangesAsync(ct);

        // Server-authoritative remaining seconds. Computed off StartedAtUtc
        // so the client cannot extend its timer by reloading.
        var remaining = 0;
        if (attempt.StartedAtUtc.HasValue)
        {
            var deadline = attempt.StartedAtUtc.Value.AddMinutes(attempt.Exam.DurationMinutes);
            remaining = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalSeconds);
        }

        return new ReconnectInfo(remaining, sessionWasNew, wasInactive);
    }

    public async Task EvaluateSuspicionAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts.FindAsync(new object[] { attemptId }, ct);
        if (attempt is null || attempt.Status != AttemptStatus.InProgress) return;

        var counts = await _db.ExamEvents
            .AsNoTracking()
            .Where(e => e.AttemptId == attemptId)
            .GroupBy(e => e.EventType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(ExamEventType t) =>
            counts.FirstOrDefault(c => c.Key == t)?.Count ?? 0;

        var tabBlurs = Count(ExamEventType.TabBlur);
        var fullscreenExits = Count(ExamEventType.FullscreenExit);

        // ---- Critical threshold: force-submit through the runtime path
        // so audit / publication / notifications stay consistent.
        var critical = tabBlurs >= CriticalTabBlurThreshold
                    || fullscreenExits >= CriticalFullscreenExitThreshold;

        if (critical)
        {
            var profile = await _db.Set<ExamSecurityProfile>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ExamId == attempt.ExamId, ct);

            // The existing schema only carries AutoSubmitOnTimeout; we
            // treat it as the consolidated "system may force-submit"
            // toggle for this exam.
            var allowAutoSubmit = profile?.AutoSubmitOnTimeout ?? true;

            _db.ExamEvents.Add(new ExamEvent
            {
                AttemptId = attemptId,
                EventType = ExamEventType.SuspiciousActivity,
                OccurredAtUtc = DateTime.UtcNow,
                DetailsJson = $"{{\"tabBlurs\":{tabBlurs},\"fullscreenExits\":{fullscreenExits},\"action\":\"{(allowAutoSubmit ? "auto-submit" : "flag-only")}\"}}"
            });
            attempt.IsSuspicious = true;
            await _db.SaveChangesAsync(ct);

            if (allowAutoSubmit)
            {
                _logger.LogWarning(
                    "Critical suspicion on attempt {AttemptId} (tabBlurs={TabBlurs}, fullscreenExits={FullscreenExits}); auto-submitting.",
                    attemptId, tabBlurs, fullscreenExits);
                await _runtime.AutoSubmitAsync(attemptId, ct);
            }
            return;
        }

        // ---- Soft threshold: flag attempt for proctor review only.
        var soft = tabBlurs >= SoftTabBlurThreshold
                || fullscreenExits >= SoftFullscreenExitThreshold;

        if (soft && !attempt.IsSuspicious)
        {
            attempt.IsSuspicious = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<ExamEventSummary>> GetTimelineAsync(
        Guid attemptId, CancellationToken ct = default)
    {
        return await _db.ExamEvents
            .AsNoTracking()
            .Where(e => e.AttemptId == attemptId)
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e => new ExamEventSummary(e.EventType, e.OccurredAtUtc, e.DetailsJson))
            .ToListAsync(ct);
    }

    private async Task<int> GetMaxConnectionLossSecondsAsync(Guid examId, CancellationToken ct)
    {
        var profile = await _db.Set<ExamSecurityProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ExamId == examId, ct);
        return profile?.MaxConnectionLossSeconds ?? 120;
    }
}
