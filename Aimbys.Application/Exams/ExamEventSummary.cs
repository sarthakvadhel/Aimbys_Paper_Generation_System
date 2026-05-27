using Aimbys.Domain.Enums;

namespace Aimbys.Application.Exams;

public sealed record ExamEventSummary(ExamEventType EventType, DateTime OccurredAtUtc, string? DetailsJson);
