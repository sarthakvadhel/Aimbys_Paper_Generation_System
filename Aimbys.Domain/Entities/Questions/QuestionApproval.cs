namespace Aimbys.Domain.Entities.Questions;

public class QuestionApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectionComment { get; set; }
}
