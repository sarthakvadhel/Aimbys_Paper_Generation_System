using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Exams;

public class ExamAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public AttemptStatus Status { get; set; } = AttemptStatus.NotStarted;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public bool AutoSubmitted { get; set; }
    public bool IsSuspicious { get; set; }
    public decimal? TotalAutoScore { get; set; }
    public Exam? Exam { get; set; }
    public ICollection<ExamAttemptAnswer> Answers { get; set; } = new List<ExamAttemptAnswer>();
}
