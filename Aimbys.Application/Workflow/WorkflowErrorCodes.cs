namespace Aimbys.Application.Workflow;

/// <summary>
/// Stable error codes returned in <see cref="WorkflowResult.ErrorCode"/>.
/// Controllers and tests pattern-match on these constants so the engine
/// can revise its messages without breaking callers.
/// </summary>
public static class WorkflowErrorCodes
{
    public const string DefinitionNotFound = "workflow.definition.not_found";
    public const string DefinitionNotPublished = "workflow.definition.not_published";
    public const string InitialStateUndefined = "workflow.definition.initial_state_undefined";
    public const string InstanceNotFound = "workflow.instance.not_found";
    public const string InstanceCompleted = "workflow.instance.completed";
    public const string InstanceAlreadyExists = "workflow.instance.already_exists";
    public const string TransitionNotAllowed = "workflow.transition.not_allowed";
    public const string PermissionDenied = "workflow.transition.permission_denied";
    public const string QueueItemNotFound = "workflow.queue.item_not_found";
}
