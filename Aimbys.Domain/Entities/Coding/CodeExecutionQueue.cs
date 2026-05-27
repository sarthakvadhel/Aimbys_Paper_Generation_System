namespace Aimbys.Domain.Entities.Coding;

public class CodeExecutionQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public int Priority { get; set; } = 1;
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? WorkerIdentifier { get; set; }
}
