using Aimbys.Domain.Enums;

namespace Aimbys.Application.Workflow;

/// <summary>
/// Read-model row returned by
/// <see cref="IWorkflowService.GetOpenQueueAsync"/>. Flattens the
/// underlying join (<c>ApprovalQueue</c> + <c>WorkflowInstance</c>) so
/// controllers can render inboxes without further EF queries.
/// </summary>
public sealed record QueueItemView
{
    public Guid QueueItemId { get; init; }
    public Guid InstanceId { get; init; }
    public string DefinitionKey { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public string SubjectType { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public string CurrentState { get; init; } = string.Empty;
    public Guid? InstituteId { get; init; }
    public string? AssignedToUserId { get; init; }
    public WorkflowQueuePriority Priority { get; init; }
    public DateTime EnqueuedAtUtc { get; init; }
    public DateTime? DueAtUtc { get; init; }
}
