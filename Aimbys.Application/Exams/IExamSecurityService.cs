using Aimbys.Domain.Enums;

namespace Aimbys.Application.Exams;

/// <summary>
/// Records exam-runtime security events, manages session heartbeats,
/// and triggers force-submit on critical suspicion thresholds. Every
/// state-mutating method enforces ownership at the service boundary so
/// a hostile caller cannot poison another student's event timeline.
/// </summary>
public interface IExamSecurityService
{
    /// <summary>
    /// Records a security event (tab blur, fullscreen exit, paste
    /// attempt, etc.) for an attempt owned by <paramref name="studentUserId"/>.
    /// Returns <c>false</c> when the attempt is not owned. Triggers
    /// <see cref="EvaluateSuspicionAsync"/> for security-relevant types.
    /// </summary>
    Task<bool> RecordEventAsync(
        Guid attemptId,
        ExamEventType eventType,
        string? detailsJson,
        string studentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes the <c>ExamSession.LastHeartbeatAtUtc</c> column.
    /// Returns <c>false</c> when the attempt is not owned. Creates the
    /// session row on the first heartbeat.
    /// </summary>
    Task<bool> RecordHeartbeatAsync(
        Guid attemptId,
        string studentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Re-binds an existing <c>ExamSession</c> row to a refreshed
    /// browser fingerprint after a reload / reconnect, records a
    /// <see cref="ExamEventType.ConnectionRestored"/> event, and returns
    /// the new server-authoritative remaining-seconds. Returns
    /// <c>null</c> when the attempt is not owned or already submitted.
    /// </summary>
    Task<ReconnectInfo?> ReconnectAsync(
        Guid attemptId,
        string studentUserId,
        string? deviceFingerprint,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default);

    /// <summary>
    /// Counts security events for the attempt and either marks the
    /// attempt suspicious (soft threshold) or fires
    /// <see cref="IExamRuntimeService.AutoSubmitAsync"/> (critical
    /// threshold). Idempotent: re-running the check on an already
    /// suspicious / submitted attempt is a no-op.
    /// </summary>
    Task EvaluateSuspicionAsync(Guid attemptId, CancellationToken ct = default);

    Task<IReadOnlyList<ExamEventSummary>> GetTimelineAsync(
        Guid attemptId, CancellationToken ct = default);
}

/// <summary>
/// Outcome of a successful reconnect. The remaining-seconds value is
/// computed against <c>StartedAtUtc + DurationMinutes</c> on the server
/// so the client cannot extend its own timer by reloading.
/// </summary>
public sealed record ReconnectInfo(
    int RemainingSeconds,
    bool SessionWasNew,
    bool WasInactive);
