namespace Aimbys.Application.Evaluation;

public record EvaluationContext(
    Guid EvaluationId,
    string? AnswerJson,
    string? CriteriaJson,
    IReadOnlyList<DraftScoreItem> DraftScores,
    string? Feedback);
