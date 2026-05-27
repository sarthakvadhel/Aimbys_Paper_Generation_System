using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities.Workflow;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Workflow;

/// <summary>
/// Default <see cref="IWorkflowService"/> implementation. The engine is
/// the single sanctioned route for state changes &mdash; controllers
/// must call into this surface and never branch on a status enum
/// directly.
///
/// Each successful transition:
/// <list type="number">
///   <item>Writes a <see cref="WorkflowTransition"/> history row.</item>
///   <item>Updates <see cref="WorkflowInstance.CurrentState"/>.</item>
///   <item>Marks the previous queue item resolved and (if the new
///         transition has a <c>queueName</c>) creates a new
///         <see cref="ApprovalQueue"/> row.</item>
///   <item>Resolves the previous state's deadline and (if the new
///         state has an escalation rule) creates a new
///         <see cref="WorkflowDeadline"/> row.</item>
///   <item>Writes an audit row via <see cref="IAuditWriter"/>.</item>
///   <item>Enqueues a <see cref="WorkflowTransitionedEvent"/> for
///         post-commit dispatch.</item>
///   <item>Persists everything atomically with a single
///         <c>SaveChangesAsync</c>.</item>
/// </list>
/// </summary>
public sealed class WorkflowEngine : IWorkflowService
{
    private const string AuditAction = "Workflow.Transitioned";

    private readonly AppDbContext _db;
    private readonly IWorkflowDefinitionRegistry _registry;
    private readonly IPermissionGuard _permissions;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IInstituteScope _instituteScope;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        AppDbContext db,
        IWorkflowDefinitionRegistry registry,
        IPermissionGuard permissions,
        IAuditWriter audit,
        DomainEventCollector events,
        UserManager<IdentityUser> userManager,
        IInstituteScope instituteScope,
        ILogger<WorkflowEngine> logger)
    {
        _db = db;
        _registry = registry;
        _permissions = permissions;
        _audit = audit;
        _events = events;
        _userManager = userManager;
        _instituteScope = instituteScope;
        _logger = logger;
    }

    public async Task<WorkflowResult> StartAsync(
        string definitionKey,
        string subjectType,
        Guid subjectId,
        string actorUserId,
        Guid? instituteId = null,
        CancellationToken cancellationToken = default)
    {
        var definition = _registry.Get(definitionKey);
        if (definition is null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.DefinitionNotFound,
                $"Workflow definition '{definitionKey}' is not registered.");
        }

        if (definition.States.Count == 0)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.InitialStateUndefined,
                $"Workflow definition '{definitionKey}' declares no states.");
        }

        // Reject a duplicate: a non-completed instance for the same subject
        // breaks the engine's "one workflow per business object" invariant.
        var existing = await _db.Set<WorkflowInstance>()
            .Where(i => i.DefinitionKey == definitionKey
                        && i.SubjectType == subjectType
                        && i.SubjectId == subjectId
                        && !i.IsCompleted)
            .Select(i => new { i.Id, i.CurrentState })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.InstanceAlreadyExists,
                $"An active workflow instance for {subjectType}:{subjectId} already exists.",
                existing.Id);
        }

        var initialState = definition.States[0];

        var instance = new WorkflowInstance
        {
            DefinitionKey = definition.Key,
            DefinitionVersion = definition.Version,
            SubjectType = subjectType,
            SubjectId = subjectId,
            InstituteId = instituteId,
            CurrentState = initialState,
            StartedAtUtc = DateTime.UtcNow,
            StartedByUserId = actorUserId
        };

        _db.Set<WorkflowInstance>().Add(instance);

        // Initial-state row in the transition history (FromState == "" sentinel).
        _db.Set<WorkflowTransition>().Add(new WorkflowTransition
        {
            InstanceId = instance.Id,
            FromState = string.Empty,
            ToState = initialState,
            ActorUserId = actorUserId,
            Comment = "Workflow started.",
            TransitionedAtUtc = DateTime.UtcNow
        });

        // Initial queue placement comes from a transition record whose
        // FromState == initialState (rare but allowed); for the start case
        // we look for an "incoming" transition definition, which a few
        // workflows model as fromState equal to the initial state.
        // The general pattern: if any transition's QueueName matches the
        // post-state, place the work item.
        await EnqueueIfMappedAsync(definition, fromState: string.Empty, toState: initialState, instance, cancellationToken);
        CreateDeadlineIfApplicable(definition, instance, initialState);

        await _audit.WriteAsync(
            "Workflow.Started",
            entityType: subjectType,
            entityId: subjectId.ToString(),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { definitionKey, instance.Id, initialState }),
            cancellationToken: cancellationToken);

        _events.Enqueue(new WorkflowTransitionedEvent
        {
            InstanceId = instance.Id,
            DefinitionKey = definition.Key,
            SubjectType = subjectType,
            SubjectId = subjectId,
            FromState = string.Empty,
            ToState = initialState,
            ActorUserId = actorUserId,
            InstituteId = instituteId
        });

        await _db.SaveChangesAsync(cancellationToken);

        return WorkflowResult.Success(instance.Id, initialState);
    }

    public async Task<WorkflowResult> TransitionAsync(
        Guid instanceId,
        string toState,
        ClaimsPrincipal actor,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _db.Set<WorkflowInstance>()
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.InstanceNotFound,
                $"Workflow instance {instanceId} not found.");
        }

        if (instance.IsCompleted)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.InstanceCompleted,
                "Workflow instance is already completed.",
                instance.Id);
        }

        var definition = _registry.Get(instance.DefinitionKey);
        if (definition is null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.DefinitionNotFound,
                $"Workflow definition '{instance.DefinitionKey}' is not registered.",
                instance.Id);
        }

        // Find the matching transition (case-insensitive on state names).
        var transition = definition.Transitions.FirstOrDefault(t =>
            string.Equals(t.FromState, instance.CurrentState, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.ToState, toState, StringComparison.OrdinalIgnoreCase));

        if (transition is null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.TransitionNotAllowed,
                $"Transition {instance.CurrentState} -> {toState} is not allowed by '{definition.Key}'.",
                instance.Id);
        }

        // Authorisation: SuperAdmin always bypasses; otherwise check role
        // then permission. Either constraint may be null in the JSON.
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;
        var isSuperAdmin = actor.IsInRole(Roles.SuperAdmin);

        if (!isSuperAdmin)
        {
            if (!string.IsNullOrEmpty(transition.RequiredRole)
                && !actor.IsInRole(transition.RequiredRole))
            {
                return WorkflowResult.Failure(
                    WorkflowErrorCodes.PermissionDenied,
                    $"Transition requires role '{transition.RequiredRole}'.",
                    instance.Id);
            }

            if (!string.IsNullOrEmpty(transition.RequiredPermission))
            {
                var hasPermission = await _permissions.HasAsync(
                    actor, transition.RequiredPermission, cancellationToken);

                if (!hasPermission)
                {
                    return WorkflowResult.Failure(
                        WorkflowErrorCodes.PermissionDenied,
                        $"Transition requires permission '{transition.RequiredPermission}'.",
                        instance.Id);
                }
            }
        }

        var fromState = instance.CurrentState;
        var resolvedToState = ResolveCanonicalStateName(definition, transition.ToState);

        // 1) History row.
        _db.Set<WorkflowTransition>().Add(new WorkflowTransition
        {
            InstanceId = instance.Id,
            FromState = fromState,
            ToState = resolvedToState,
            ActorUserId = actorUserId,
            Comment = comment,
            TransitionedAtUtc = DateTime.UtcNow
        });

        // 2) Update instance state and (if terminal) mark complete.
        instance.CurrentState = resolvedToState;
        if (definition.TerminalStates.Any(s =>
                string.Equals(s, resolvedToState, StringComparison.OrdinalIgnoreCase)))
        {
            instance.IsCompleted = true;
            instance.CompletedAtUtc = DateTime.UtcNow;
        }

        // 3) Queue handover: resolve the prior queue item, then place the
        //    new one if the transition record carries a QueueName.
        await ResolveOpenQueueItemsAsync(instance.Id, cancellationToken);
        if (!string.IsNullOrEmpty(transition.QueueName))
        {
            _db.Set<ApprovalQueue>().Add(new ApprovalQueue
            {
                InstanceId = instance.Id,
                DefinitionKey = definition.Key,
                QueueName = transition.QueueName,
                InstituteId = instance.InstituteId,
                EnqueuedAtUtc = DateTime.UtcNow
            });
        }

        // 4) Deadline handover.
        await ResolveOpenDeadlinesAsync(instance.Id, cancellationToken);
        CreateDeadlineIfApplicable(definition, instance, resolvedToState);

        // 5) Audit.
        await _audit.WriteAsync(
            AuditAction,
            entityType: instance.SubjectType,
            entityId: instance.SubjectId.ToString(),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new
            {
                definitionKey = definition.Key,
                instanceId = instance.Id,
                fromState,
                toState = resolvedToState,
                comment
            }),
            cancellationToken: cancellationToken);

        // 6) Domain event for projections.
        _events.Enqueue(new WorkflowTransitionedEvent
        {
            InstanceId = instance.Id,
            DefinitionKey = definition.Key,
            SubjectType = instance.SubjectType,
            SubjectId = instance.SubjectId,
            FromState = fromState,
            ToState = resolvedToState,
            ActorUserId = actorUserId,
            Comment = comment,
            InstituteId = instance.InstituteId
        });

        await _db.SaveChangesAsync(cancellationToken);

        return WorkflowResult.Success(instance.Id, resolvedToState);
    }

    public async Task<string?> GetCurrentStateAsync(
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<WorkflowInstance>()
            .AsNoTracking()
            .Where(i => i.SubjectType == subjectType && i.SubjectId == subjectId)
            .OrderByDescending(i => i.StartedAtUtc)
            .Select(i => i.CurrentState)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QueueItemView>> GetOpenQueueAsync(
        string definitionKey,
        string queueName,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;
        var isSuperAdmin = actor.IsInRole(Roles.SuperAdmin);
        var isInstituteAdmin = actor.IsInRole(Roles.InstituteAdmin);
        var instituteId = await _instituteScope.GetCurrentInstituteIdAsync(actor, cancellationToken);

        var query =
            from q in _db.Set<ApprovalQueue>().AsNoTracking()
            join i in _db.Set<WorkflowInstance>().AsNoTracking() on q.InstanceId equals i.Id
            where q.DefinitionKey == definitionKey
                  && q.QueueName == queueName
                  && !q.IsResolved
            select new { q, i };

        // Tenancy. SuperAdmin sees all; InstituteAdmin (and below) are
        // bounded by their tenant. SuperAdmin's instituteId resolves to
        // null so the IInstituteScope contract is preserved.
        if (!isSuperAdmin && instituteId.HasValue)
        {
            var scopeId = instituteId.Value;
            query = query.Where(x => x.q.InstituteId == scopeId);
        }
        else if (!isSuperAdmin)
        {
            // Authenticated user with no resolvable institute scope sees nothing.
            return Array.Empty<QueueItemView>();
        }

        // Visibility: SuperAdmin / InstituteAdmin see everything in scope;
        // everyone else sees items pre-assigned to themselves OR unassigned.
        if (!isSuperAdmin && !isInstituteAdmin)
        {
            query = query.Where(x =>
                x.q.AssignedToUserId == null || x.q.AssignedToUserId == actorUserId);
        }

        var rows = await query
            .OrderByDescending(x => (int)x.q.Priority)
            .ThenBy(x => x.q.EnqueuedAtUtc)
            .Select(x => new QueueItemView
            {
                QueueItemId = x.q.Id,
                InstanceId = x.q.InstanceId,
                DefinitionKey = x.q.DefinitionKey,
                QueueName = x.q.QueueName,
                SubjectType = x.i.SubjectType,
                SubjectId = x.i.SubjectId,
                CurrentState = x.i.CurrentState,
                InstituteId = x.q.InstituteId,
                AssignedToUserId = x.q.AssignedToUserId,
                Priority = x.q.Priority,
                EnqueuedAtUtc = x.q.EnqueuedAtUtc,
                DueAtUtc = x.q.DueAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<WorkflowResult> AssignAsync(
        Guid queueItemId,
        string assignToUserId,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.Set<ApprovalQueue>()
            .FirstOrDefaultAsync(q => q.Id == queueItemId, cancellationToken);

        if (item is null)
        {
            return WorkflowResult.Failure(
                WorkflowErrorCodes.QueueItemNotFound,
                $"Queue item {queueItemId} not found.");
        }

        // Supersede any active prior assignments for the same queue item.
        var prior = await _db.Set<TaskAssignment>()
            .Where(a => a.QueueItemId == queueItemId && a.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var p in prior)
        {
            p.IsActive = false;
        }

        _db.Set<TaskAssignment>().Add(new TaskAssignment
        {
            QueueItemId = queueItemId,
            AssignedToUserId = assignToUserId,
            AssignedByUserId = actorUserId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        });

        item.AssignedToUserId = assignToUserId;

        await _audit.WriteAsync(
            "Workflow.QueueItemAssigned",
            entityType: "ApprovalQueue",
            entityId: queueItemId.ToString(),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { queueItemId, assignToUserId }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return WorkflowResult.Success(item.InstanceId, item.QueueName);
    }

    // ----- helpers ------------------------------------------------------

    private async Task ResolveOpenQueueItemsAsync(Guid instanceId, CancellationToken ct)
    {
        var open = await _db.Set<ApprovalQueue>()
            .Where(q => q.InstanceId == instanceId && !q.IsResolved)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var q in open)
        {
            q.IsResolved = true;
            q.ResolvedAtUtc = now;
        }
    }

    private async Task ResolveOpenDeadlinesAsync(Guid instanceId, CancellationToken ct)
    {
        var open = await _db.Set<WorkflowDeadline>()
            .Where(d => d.InstanceId == instanceId && !d.IsResolved)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var d in open)
        {
            d.IsResolved = true;
            d.ResolvedAtUtc = now;
        }
    }

    private void CreateDeadlineIfApplicable(
        WorkflowDefinitionDocument definition,
        WorkflowInstance instance,
        string state)
    {
        var rule = definition.EscalationRules.FirstOrDefault(r =>
            string.Equals(r.State, state, StringComparison.OrdinalIgnoreCase));

        if (rule is null) return;

        _db.Set<WorkflowDeadline>().Add(new WorkflowDeadline
        {
            InstanceId = instance.Id,
            State = state,
            InstituteId = instance.InstituteId,
            DueAtUtc = DateTime.UtcNow.AddMinutes(rule.MaxDurationMinutes),
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Look for a transition whose target is the new state and whose
    /// queueName is set. Used during <see cref="StartAsync"/> when the
    /// initial state itself has an associated inbox.
    /// </summary>
    private Task EnqueueIfMappedAsync(
        WorkflowDefinitionDocument definition,
        string fromState,
        string toState,
        WorkflowInstance instance,
        CancellationToken ct)
    {
        var match = definition.Transitions.FirstOrDefault(t =>
            string.Equals(t.FromState, fromState, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.ToState, toState, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(t.QueueName));

        if (match is null) return Task.CompletedTask;

        _db.Set<ApprovalQueue>().Add(new ApprovalQueue
        {
            InstanceId = instance.Id,
            DefinitionKey = definition.Key,
            QueueName = match.QueueName!,
            InstituteId = instance.InstituteId,
            EnqueuedAtUtc = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the canonically-cased state name as declared in the
    /// definition. Keeps stored values consistent even if the caller
    /// passes a differently-cased toState.
    /// </summary>
    private static string ResolveCanonicalStateName(WorkflowDefinitionDocument definition, string toState)
    {
        var canonical = definition.States.FirstOrDefault(s =>
            string.Equals(s, toState, StringComparison.OrdinalIgnoreCase));
        return canonical ?? toState;
    }
}
