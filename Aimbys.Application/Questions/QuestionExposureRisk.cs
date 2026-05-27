namespace Aimbys.Application.Questions;

public sealed record QuestionExposureRisk(
    int ExposureCount,
    string RiskLevel); // "Low", "Medium", "High"
