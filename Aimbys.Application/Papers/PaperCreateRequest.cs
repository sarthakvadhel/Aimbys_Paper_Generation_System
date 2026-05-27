namespace Aimbys.Application.Papers;

public sealed record PaperCreateRequest(
    string Title,
    Guid SubjectId,
    int TotalMarks,
    int DurationMinutes);
