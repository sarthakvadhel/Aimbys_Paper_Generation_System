namespace Aimbys.Application.Results;

public record StudentResultView(
    Guid AttemptId,
    decimal TotalScore,
    decimal MaxScore,
    double Percentage,
    string? Grade,
    int? Rank,
    IReadOnlyList<AnswerScoreItem> Answers);
