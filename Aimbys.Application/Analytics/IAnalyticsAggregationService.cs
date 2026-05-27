namespace Aimbys.Application.Analytics;

public interface IAnalyticsAggregationService
{
    Task AggregateInstituteMetricsAsync(Guid instituteId, CancellationToken ct = default);
    Task AggregateStudentPerformanceAsync(Guid instituteId, CancellationToken ct = default);
    Task AggregateEvaluatorEfficiencyAsync(Guid instituteId, CancellationToken ct = default);
    Task RecomputeLeaderboardAsync(Guid examId, CancellationToken ct = default);
}
