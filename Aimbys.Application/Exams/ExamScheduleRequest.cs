namespace Aimbys.Application.Exams;

public sealed record ExamScheduleRequest(
    Guid PaperVersionId,
    Guid ClassBatchId,
    string Title,
    DateTime ScheduledAtUtc,
    int DurationMinutes);
