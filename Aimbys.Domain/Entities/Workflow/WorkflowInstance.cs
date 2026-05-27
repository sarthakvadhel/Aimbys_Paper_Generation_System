namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Runtime tracker that binds a <see cref="WorkflowDefinition"/> to a
/// specific business object. Identified by
/// (<see cref="SubjectType"/>, <see cref="SubjectId"/>); subsequent
/// transitions update <see cref="CurrentState"/> in-place while
/// <see cref="WorkflowTransition"/> rows preserve full history.
///
/// Tenancy: <see cref="InstituteId"/> is denormalised onto the instance
/// so escalation queries don't need a join through the subject's owning
/// entity. The calling service is responsible for setting it correctly.
/// </summary>
public class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="WorkflowDefinition.Key"/>.</summary>
    public string DefinitionKey { get; set; } = string.Empty;

    /// <summary>Captured at start so later definition revisions don't break running instances.</summary>
    public int DefinitionVersion { get; set; } = 1;

    /// <summary>
    /// Discriminator naming the business entity, e.g. <c>"Institute"</c>,
    /// <c>"Question"</c>, <c>"Paper"</c>, <c>"Evaluation"</c>.
    /// </summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Primary key of the business entity.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Tenancy boundary; null for platform-level workflows (e.g. institute approval).</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>Current state name. Set to the definition's initial state at start.</summary>
    public string CurrentState { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the instance reaches a terminal state.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// True once the instance reaches a terminal state. Completed
    /// instances are read-only; subsequent <c>TransitionAsync</c> calls
    /// against them must fail.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>Identity user id of the actor that started the workflow.</summary>
    public string StartedByUserId { get; set; } = string.Empty;

    // Navigation
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}
