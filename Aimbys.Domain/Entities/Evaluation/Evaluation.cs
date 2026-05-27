using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Evaluation;

public class Evaluation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptAnswerId { get; set; }
    public Guid EvaluatorTeacherProfileId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public EvaluationStatus Status { get; set; } = EvaluationStatus.Pending;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? Feedback { get; set; }
}
