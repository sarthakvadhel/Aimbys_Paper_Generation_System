namespace Aimbys.Application.Evaluation;

public interface IEvaluationService
{
    Task<bool> SaveDraftScoreAsync(Guid evaluationId, int criterionIndex, decimal points, string actorUserId, CancellationToken ct = default);
    Task<bool> SaveFeedbackDraftAsync(Guid evaluationId, string feedback, string actorUserId, CancellationToken ct = default);
    Task<EvaluationSubmitResult> SubmitAsync(Guid evaluationId, string actorUserId, CancellationToken ct = default);
    Task<EvaluationContext?> GetScoringContextAsync(Guid evaluationId, CancellationToken ct = default);
}
