using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Results;

public class ResultAppeal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamAttemptAnswerId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AppealStatus Status { get; set; } = AppealStatus.Open;
    public Guid? WorkflowInstanceId { get; set; }
    public DateTime FiledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
