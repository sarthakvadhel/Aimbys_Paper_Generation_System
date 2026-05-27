namespace Aimbys.Application.Papers;

public sealed record PaperSaveRequest(
    IReadOnlyList<PaperSectionInput> Sections,
    IReadOnlyList<PaperQuestionInput> Questions);

public sealed record PaperSectionInput(
    string Name,
    int Marks,
    int SortOrder);

public sealed record PaperQuestionInput(
    int SectionIndex,
    Guid QuestionId,
    Guid QuestionVersionId,
    int SortOrder,
    decimal? MarksOverride);
