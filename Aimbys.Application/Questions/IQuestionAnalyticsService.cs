namespace Aimbys.Application.Questions;

public interface IQuestionAnalyticsService
{
    Task RecordExposureAsync(Guid questionId, Guid paperId, Guid instituteId, CancellationToken ct = default);
    Task<QuestionUsageSummary?> GetUsageSummaryAsync(Guid questionId, CancellationToken ct = default);
    Task<QuestionExposureRisk> GetExposureRiskAsync(Guid questionId, CancellationToken ct = default);
}
