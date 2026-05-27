using Aimbys.Domain.Enums;

namespace Aimbys.Application.Exams;

public interface IExamSecurityService
{
    Task<bool> RecordEventAsync(Guid attemptId, ExamEventType eventType, string? detailsJson, string studentUserId, CancellationToken ct = default);
    Task<bool> RecordHeartbeatAsync(Guid attemptId, string studentUserId, CancellationToken ct = default);
    Task EvaluateSuspicionAsync(Guid attemptId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamEventSummary>> GetTimelineAsync(Guid attemptId, CancellationToken ct = default);
}
