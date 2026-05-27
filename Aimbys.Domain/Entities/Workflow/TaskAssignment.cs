namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Explicit assignment of an <see cref="ApprovalQueue"/> item to a
/// specific user. Created via <c>IWorkflowService.AssignAsync</c>.
/// Append-only: re-assignment writes a new row and supersedes the
/// previous one (selectable via <see cref="IsActive"/>).
/// </summary>
public class TaskAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="ApprovalQueue.Id"/>.</summary>
    public Guid QueueItemId { get; set; }

    /// <summary>FK to <c>AspNetUsers.Id</c>; the user the work was assigned to.</summary>
    public string AssignedToUserId { get; set; } = string.Empty;

    /// <summary>FK to <c>AspNetUsers.Id</c>; the user (admin/HoD) that performed the assignment.</summary>
    public string AssignedByUserId { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True for the most recent assignment; flipped to <c>false</c> when
    /// the queue item is reassigned. Audit trail is preserved.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ApprovalQueue? QueueItem { get; set; }
}
