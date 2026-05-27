namespace Aimbys.Application.Blueprints;

public sealed record BlueprintCreateRequest(
    string Name,
    Guid SubjectId,
    Guid? AssessmentDesignId,
    int TotalMarks,
    int DurationMinutes);
