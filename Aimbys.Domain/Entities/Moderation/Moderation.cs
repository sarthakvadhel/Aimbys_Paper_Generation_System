using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Moderation;

public class Moderation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvaluationId { get; set; }
    public Guid ModeratorTeacherProfileId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public ModerationVerdict Verdict { get; set; } = ModerationVerdict.Pending;
    public string? Comment { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
