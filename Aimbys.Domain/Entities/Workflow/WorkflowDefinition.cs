namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Versioned, immutable specification of a state machine. Identified by
/// <see cref="Key"/> + <see cref="Version"/>; once published, a definition
/// is never edited &mdash; new business rules ship as a new version so
/// running instances retain their original semantics.
///
/// The actual states/transitions/escalation rules are stored as JSON
/// strings (sourced from the embedded resources under
/// <c>Aimbys.Infrastructure/Workflow/Definitions/*.json</c>) so the
/// runtime payload can evolve without schema migrations.
/// </summary>
public class WorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable, machine-readable key (e.g. <c>"InstituteApproval"</c>).
    /// Unique with <see cref="Version"/>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Monotonically increasing version starting at 1.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Human-readable description shown in admin tooling.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of state names. The first element is the initial state.
    /// </summary>
    public string StatesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of transition records. Each record carries
    /// <c>fromState</c>, <c>toState</c>, optional <c>requiredRole</c>,
    /// optional <c>requiredPermission</c>, and an optional
    /// <c>queueName</c> the post-transition state should be enqueued to.
    /// </summary>
    public string TransitionsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of escalation rules. Each record carries
    /// <c>state</c>, <c>maxDurationMinutes</c>,
    /// <c>reminderAtPercent</c>, and the role/permission to escalate to.
    /// May be an empty array.
    /// </summary>
    public string EscalationRulesJson { get; set; } = "[]";

    /// <summary>
    /// True once a definition is fully validated and registered; false
    /// while a draft is being staged. Only published definitions are
    /// usable by <c>IWorkflowService.StartAsync</c>.
    /// </summary>
    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
}
