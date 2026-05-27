namespace Aimbys.Application.Questions;

public sealed record QuestionUsageSummary(
    int PapersUsedIn,
    int AttemptsCount,
    double PValue,
    double DiscriminationIndex,
    double MeanTimeSeconds,
    DateTime ComputedAtUtc);
