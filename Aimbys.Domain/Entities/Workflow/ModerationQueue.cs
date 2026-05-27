using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Queue of post-evaluation moderation items. Distinct from
/// <see cref="ApprovalQueue"/> because the moderation workflow has
/// different roles (Moderator), different SLA tiers, and feeds the
/// <c>Moderation.Returned</c> domain event when items go back to the
/// evaluator.
/// </summary>
public class ModerationQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to the evaluation being moderated.</summary>
    public Guid EvaluationId { get; set; }

    /// <summary>Tenancy boundary.</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>FK to <c>AspNetUsers.Id</c>; the moderator the work landed with.</summary>
    public string? ModeratorUserId { get; set; }

    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>SLA deadline, if any.</summary>
    public DateTime? DueAtUtc { get; set; }

    public WorkflowQueuePriority Priority { get; set; } = WorkflowQueuePriority.Normal;

    /// <summary>True once moderation finishes (approved, requires-changes, or overridden).</summary>
    public bool IsResolved { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
}
