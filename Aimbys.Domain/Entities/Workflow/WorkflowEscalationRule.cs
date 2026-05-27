namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Per-definition escalation specification. Sourced from the definition's
/// embedded JSON resource at startup and mirrored into this table so
/// the escalation job can run a SQL query without parsing JSON for
/// every check.
/// </summary>
public class WorkflowEscalationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="WorkflowDefinition.Key"/>.</summary>
    public string DefinitionKey { get; set; } = string.Empty;

    /// <summary>State the rule applies to.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Maximum allowed time-in-state before escalation, in minutes.</summary>
    public int MaxDurationMinutes { get; set; }

    /// <summary>
    /// Percent of <see cref="MaxDurationMinutes"/> that triggers a
    /// reminder (e.g. <c>75</c> means a reminder is sent at 75% of the
    /// SLA window). 0 disables the reminder.
    /// </summary>
    public int ReminderAtPercent { get; set; } = 75;

    /// <summary>Identity role the escalation hands off to (e.g. <c>"InstituteAdmin"</c>). Optional.</summary>
    public string? EscalateToRole { get; set; }

    /// <summary>Permission key the escalation requires. Optional.</summary>
    public string? EscalateToPermission { get; set; }
}
