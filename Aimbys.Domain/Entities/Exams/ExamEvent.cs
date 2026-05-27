using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Exams;

public class ExamEvent
{
    public long Id { get; set; }
    public Guid AttemptId { get; set; }
    public ExamEventType EventType { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? DetailsJson { get; set; }
    public ExamAttempt? Attempt { get; set; }
}
