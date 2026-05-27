using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Exams;

public class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public Guid PaperVersionId { get; set; }
    public Guid ClassBatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Scheduled;
    public string ScheduledByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
