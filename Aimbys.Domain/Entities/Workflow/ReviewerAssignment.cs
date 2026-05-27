namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Specialised assignment used by reviewer/evaluator dispatch. Carries
/// load-balancing metadata so the dispatcher can pick the least-loaded
/// reviewer for the next subject. One row per (subject, reviewer)
/// pairing; <see cref="CurrentLoad"/> reflects open assignments at
/// dispatch time.
/// </summary>
public class ReviewerAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to a workflow subject (typically a <c>Question</c> or
    /// <c>Evaluation</c> id, depending on the calling workflow).
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>Discriminator for the subject type, e.g. <c>"Question"</c>.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Tenancy boundary.</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>FK to <c>AspNetUsers.Id</c>; the reviewer.</summary>
    public string ReviewerUserId { get; set; } = string.Empty;

    /// <summary>FK to <c>AspNetUsers.Id</c>; the dispatcher (admin or HoD).</summary>
    public string AssignedByUserId { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Snapshot of how many open reviewer assignments the reviewer held
    /// at dispatch time. Used by future load-balancing queries; not
    /// updated retroactively.
    /// </summary>
    public int CurrentLoad { get; set; }

    /// <summary>True until the reviewer completes (or is reassigned off) the subject.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime? CompletedAtUtc { get; set; }
}
