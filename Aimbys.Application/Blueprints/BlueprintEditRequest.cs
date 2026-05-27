namespace Aimbys.Application.Blueprints;

public sealed record BlueprintEditRequest(
    int TotalMarks,
    int DurationMinutes,
    IReadOnlyList<BlueprintSectionDto> Sections,
    IReadOnlyList<BlueprintConstraintDto> Constraints,
    IReadOnlyList<BlueprintCohortDto> Cohorts);

public sealed record BlueprintSectionDto(
    string Name,
    int Marks,
    int QuestionCount,
    string? TypeMix,
    int SortOrder);

public sealed record BlueprintConstraintDto(
    Guid ChapterId,
    Guid? CompetencyId,
    int DifficultyLevel,
    int QuestionType,
    int Marks,
    int Count);

public sealed record BlueprintCohortDto(
    Guid? StreamId,
    Guid? MajorId,
    Guid? DepartmentId,
    Guid? AcademicYearId,
    Guid? ClassBatchId);
