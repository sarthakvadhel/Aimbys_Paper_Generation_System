using System.Text.Json.Serialization;

namespace Aimbys.Infrastructure.Workflow;

/// <summary>
/// JSON shape for an embedded workflow-definition resource. Deserialised
/// from <c>Aimbys.Infrastructure/Workflow/Definitions/*.json</c> on
/// startup.
/// </summary>
public sealed class WorkflowDefinitionDocument
{
    /// <summary>Stable workflow key, e.g. <c>"PaperApproval"</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>State names. Element 0 is the initial state.</summary>
    [JsonPropertyName("states")]
    public List<string> States { get; set; } = new();

    /// <summary>Set of state names that mark the instance complete.</summary>
    [JsonPropertyName("terminalStates")]
    public List<string> TerminalStates { get; set; } = new();

    [JsonPropertyName("transitions")]
    public List<WorkflowTransitionDocument> Transitions { get; set; } = new();

    [JsonPropertyName("escalationRules")]
    public List<WorkflowEscalationRuleDocument> EscalationRules { get; set; } = new();
}

/// <summary>One transition record inside a definition document.</summary>
public sealed class WorkflowTransitionDocument
{
    [JsonPropertyName("fromState")]
    public string FromState { get; set; } = string.Empty;

    [JsonPropertyName("toState")]
    public string ToState { get; set; } = string.Empty;

    /// <summary>Identity role that may perform this transition. Optional (null = any authenticated actor).</summary>
    [JsonPropertyName("requiredRole")]
    public string? RequiredRole { get; set; }

    /// <summary>Permission key the actor must hold (checked via <c>IPermissionGuard</c>). Optional.</summary>
    [JsonPropertyName("requiredPermission")]
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// When set, after the transition the engine enqueues an
    /// <c>ApprovalQueue</c> row in this queue. Null means the post-state
    /// has no associated inbox.
    /// </summary>
    [JsonPropertyName("queueName")]
    public string? QueueName { get; set; }
}

/// <summary>One escalation rule inside a definition document.</summary>
public sealed class WorkflowEscalationRuleDocument
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("maxDurationMinutes")]
    public int MaxDurationMinutes { get; set; }

    [JsonPropertyName("reminderAtPercent")]
    public int ReminderAtPercent { get; set; } = 75;

    [JsonPropertyName("escalateToRole")]
    public string? EscalateToRole { get; set; }

    [JsonPropertyName("escalateToPermission")]
    public string? EscalateToPermission { get; set; }
}
