namespace Aimbys.Application.Results;

public record AnswerScoreItem(Guid AnswerId, decimal PointsAwarded, decimal MaxPoints, string Source);
