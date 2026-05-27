using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Open work item awaiting human action. The combination
/// (<see cref="DefinitionKey"/>, <see cref="QueueName"/>) groups items
/// into named inboxes (e.g. <c>QuestionApproval / SubjectExpertReview</c>);
/// transitions in the underlying definition specify which queue the
/// post-state lands in.
/// </summary>
public class ApprovalQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="WorkflowInstance.Id"/>.</summary>
    public Guid InstanceId { get; set; }

    /// <summary>Mirrors <see cref="WorkflowInstance.DefinitionKey"/> for fast filtering.</summary>
    public string DefinitionKey { get; set; } = string.Empty;

    /// <summary>Logical inbox name within a definition, e.g. <c>"DepartmentApproval"</c>.</summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>Tenancy boundary for the queue row.</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>Optional pre-assigned user. Null means "any actor with the role/permission".</summary>
    public string? AssignedToUserId { get; set; }

    public WorkflowQueuePriority Priority { get; set; } = WorkflowQueuePriority.Normal;

    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional SLA deadline shadowed from <see cref="WorkflowDeadline"/>.</summary>
    public DateTime? DueAtUtc { get; set; }

    /// <summary>True once the underlying instance has transitioned out of this queue.</summary>
    public bool IsResolved { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    // Navigation
    public WorkflowInstance? Instance { get; set; }
}
