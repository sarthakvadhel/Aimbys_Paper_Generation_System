namespace Aimbys.Application.Workflow;

/// <summary>
/// Outcome of a workflow operation. Carries either a success payload (the
/// instance id and the new current state) or an error code + message.
/// Returning a result instead of throwing keeps controller code
/// declarative &mdash; the engine never throws for a domain rejection
/// (invalid transition, insufficient permission, completed instance).
/// </summary>
public sealed record WorkflowResult
{
    public bool IsSuccess { get; init; }

    /// <summary>Stable, machine-readable error code; null on success.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Human-readable message; non-null on failure.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Workflow instance id, set on both success and failure when known.</summary>
    public Guid? InstanceId { get; init; }

    /// <summary>State the instance now sits in. Set on success, null on failure.</summary>
    public string? CurrentState { get; init; }

    public static WorkflowResult Success(Guid instanceId, string currentState) => new()
    {
        IsSuccess = true,
        InstanceId = instanceId,
        CurrentState = currentState
    };

    public static WorkflowResult Failure(string code, string message, Guid? instanceId = null) => new()
    {
        IsSuccess = false,
        ErrorCode = code,
        ErrorMessage = message,
        InstanceId = instanceId
    };
}
