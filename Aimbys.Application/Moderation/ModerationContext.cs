using Aimbys.Domain.Enums;

namespace Aimbys.Application.Moderation;

public sealed record ModerationContext(
    Guid ModerationId,
    Guid EvaluationId,
    string? EvaluatorScoresJson,
    string? AnswerJson,
    ModerationVerdict Verdict,
    decimal? EvaluatedTotal,
    decimal? MaxPossible);
