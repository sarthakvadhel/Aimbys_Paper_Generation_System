using System.Security.Claims;

namespace Aimbys.Application.Workflow;

/// <summary>
/// Generic state-machine surface for every lifecycle in the platform.
///
/// Controllers <em>must</em> use this interface for state changes &mdash;
/// no controller may inspect a status enum and branch on it. Each
/// implementation:
///
/// <list type="bullet">
///   <item>Validates transitions against the active
///         <c>WorkflowDefinition</c> for the instance's
///         <c>(DefinitionKey, DefinitionVersion)</c>.</item>
///   <item>Checks the actor's role + permission against the
///         transition's allowed-actors list (<see cref="IPermissionGuard"/>).</item>
///   <item>Writes the <c>WorkflowTransition</c> history row.</item>
///   <item>Updates <c>WorkflowInstance.CurrentState</c> (and
///         <c>IsCompleted</c> for terminal states).</item>
///   <item>Resolves the previous queue item and enqueues the next one
///         (when the new state has a queue mapping).</item>
///   <item>Creates / resolves <c>WorkflowDeadline</c> rows based on the
///         escalation rules attached to the new state.</item>
///   <item>Writes an <c>AuditLog</c> row (<c>"Workflow.Transitioned"</c>).</item>
///   <item>Enqueues a <see cref="Aimbys.Domain.Events.WorkflowTransitionedEvent"/>
///         for post-commit dispatch by the domain-event interceptor.</item>
/// </list>
///
/// All methods return a <see cref="WorkflowResult"/> instead of throwing
/// so callers can distinguish domain rejections from infrastructure
/// failures.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Starts a new workflow instance for the given subject. Returns
    /// failure if a non-completed instance already exists for the
    /// (subjectType, subjectId) pair.
    /// </summary>
    Task<WorkflowResult> StartAsync(
        string definitionKey,
        string subjectType,
        Guid subjectId,
        string actorUserId,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions the instance into <paramref name="toState"/>. Validates
    /// the transition against the definition and the actor's permissions.
    /// </summary>
    Task<WorkflowResult> TransitionAsync(
        Guid instanceId,
        string toState,
        ClaimsPrincipal actor,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current state name for the workflow attached to
    /// (subjectType, subjectId), or <c>null</c> if no instance exists.
    /// </summary>
    Task<string?> GetCurrentStateAsync(
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns open queue items for the named (definition, queue),
    /// filtered by the actor's tenancy and assignment scope:
    /// SuperAdmin/InstituteAdmin see all items in scope; everyone else
    /// sees only items pre-assigned to them or unassigned items their
    /// role/permission allows.
    /// </summary>
    Task<IReadOnlyList<QueueItemView>> GetOpenQueueAsync(
        string definitionKey,
        string queueName,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pre-assigns a queue item to a specific user. Creates a fresh
    /// <c>TaskAssignment</c> row and supersedes any active prior
    /// assignment for the same queue item.
    /// </summary>
    Task<WorkflowResult> AssignAsync(
        Guid queueItemId,
        string assignToUserId,
        string actorUserId,
        CancellationToken cancellationToken = default);
}
