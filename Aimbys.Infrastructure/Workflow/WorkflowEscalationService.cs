using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Notifications;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Workflow;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Workflow;

/// <summary>
/// Default <see cref="IWorkflowEscalationService"/>. Sweeps
/// <see cref="WorkflowDeadline"/> rows on a cadence (target: hourly,
/// invoked by <c>ISchedulingService</c> in a future chunk).
///
/// For each unresolved deadline, the service:
/// <list type="bullet">
///   <item>Marks the row <c>IsOverdue</c> when <c>UtcNow > DueAtUtc</c>.</item>
///   <item>Sends a reminder when the configured
///         <c>ReminderAtPercent</c> threshold is crossed and no reminder
///         has been sent yet.</item>
///   <item>Sends an escalation when the deadline has passed and no
///         escalation has been sent yet.</item>
/// </list>
///
/// Both <see cref="SendReminderAsync"/> and <see cref="EscalateAsync"/>
/// are idempotent: re-running the sweep on the same deadlines does not
/// produce duplicate notifications.
/// </summary>
public sealed class WorkflowEscalationService : IWorkflowEscalationService
{
    private readonly AppDbContext _db;
    private readonly IWorkflowDefinitionRegistry _registry;
    private readonly INotificationService _notifications;
    private readonly IAuditWriter _audit;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<WorkflowEscalationService> _logger;

    public WorkflowEscalationService(
        AppDbContext db,
        IWorkflowDefinitionRegistry registry,
        INotificationService notifications,
        IAuditWriter audit,
        UserManager<IdentityUser> userManager,
        ILogger<WorkflowEscalationService> logger)
    {
        _db = db;
        _registry = registry;
        _notifications = notifications;
        _audit = audit;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(int RemindersSent, int EscalationsSent)> CheckDeadlinesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var deadlines = await _db.Set<WorkflowDeadline>()
            .Where(d => !d.IsResolved)
            .ToListAsync(cancellationToken);

        if (deadlines.Count == 0)
        {
            return (0, 0);
        }

        // Cache instance lookups so we can read DefinitionKey + state ownership.
        var instanceIds = deadlines.Select(d => d.InstanceId).Distinct().ToList();
        var instances = await _db.Set<WorkflowInstance>()
            .Where(i => instanceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var reminders = 0;
        var escalations = 0;

        foreach (var deadline in deadlines)
        {
            if (!instances.TryGetValue(deadline.InstanceId, out var instance))
            {
                continue;
            }

            // If the instance has moved on from the tracked state the
            // deadline no longer applies — close it out.
            if (!string.Equals(instance.CurrentState, deadline.State, StringComparison.OrdinalIgnoreCase))
            {
                deadline.IsResolved = true;
                deadline.ResolvedAtUtc = now;
                continue;
            }

            var definition = _registry.Get(instance.DefinitionKey);
            var rule = definition?.EscalationRules.FirstOrDefault(r =>
                string.Equals(r.State, deadline.State, StringComparison.OrdinalIgnoreCase));

            if (rule is null)
            {
                // Stale deadline whose definition / state has dropped its
                // escalation rule — resolve and move on.
                deadline.IsResolved = true;
                deadline.ResolvedAtUtc = now;
                continue;
            }

            if (now > deadline.DueAtUtc)
            {
                deadline.IsOverdue = true;

                if (deadline.EscalatedAtUtc is null)
                {
                    var sent = await EscalateInternalAsync(deadline, instance, rule, now, cancellationToken);
                    if (sent) escalations++;
                }
                continue;
            }

            // Reminder threshold = CreatedAtUtc + window * (percent / 100).
            if (rule.ReminderAtPercent > 0 && deadline.ReminderSentAtUtc is null)
            {
                var window = deadline.DueAtUtc - deadline.CreatedAtUtc;
                var reminderAt = deadline.CreatedAtUtc + TimeSpan.FromMinutes(
                    window.TotalMinutes * (rule.ReminderAtPercent / 100.0));

                if (now >= reminderAt)
                {
                    var sent = await SendReminderInternalAsync(deadline, instance, rule, now, cancellationToken);
                    if (sent) reminders++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow escalation sweep completed. Reminders={Reminders}, Escalations={Escalations}",
            reminders, escalations);

        return (reminders, escalations);
    }

    public async Task SendReminderAsync(Guid deadlineId, CancellationToken cancellationToken = default)
    {
        var (deadline, instance, rule) = await LoadAsync(deadlineId, cancellationToken);
        if (deadline is null || instance is null || rule is null) return;
        if (deadline.ReminderSentAtUtc is not null) return; // idempotent

        await SendReminderInternalAsync(deadline, instance, rule, DateTime.UtcNow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task EscalateAsync(Guid deadlineId, CancellationToken cancellationToken = default)
    {
        var (deadline, instance, rule) = await LoadAsync(deadlineId, cancellationToken);
        if (deadline is null || instance is null || rule is null) return;
        if (deadline.EscalatedAtUtc is not null) return; // idempotent

        await EscalateInternalAsync(deadline, instance, rule, DateTime.UtcNow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- helpers ------------------------------------------------------

    private async Task<(WorkflowDeadline? deadline, WorkflowInstance? instance, WorkflowEscalationRuleDocument? rule)>
        LoadAsync(Guid deadlineId, CancellationToken ct)
    {
        var deadline = await _db.Set<WorkflowDeadline>()
            .FirstOrDefaultAsync(d => d.Id == deadlineId, ct);

        if (deadline is null) return (null, null, null);

        var instance = await _db.Set<WorkflowInstance>()
            .FirstOrDefaultAsync(i => i.Id == deadline.InstanceId, ct);

        if (instance is null) return (deadline, null, null);

        var rule = _registry.Get(instance.DefinitionKey)?.EscalationRules
            .FirstOrDefault(r => string.Equals(r.State, deadline.State, StringComparison.OrdinalIgnoreCase));

        return (deadline, instance, rule);
    }

    private async Task<bool> SendReminderInternalAsync(
        WorkflowDeadline deadline,
        WorkflowInstance instance,
        WorkflowEscalationRuleDocument rule,
        DateTime now,
        CancellationToken ct)
    {
        var recipients = await ResolveRemindRecipientsAsync(instance, deadline, ct);
        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Reminder for deadline {DeadlineId} ({Definition}/{State}) has no recipients. Skipping.",
                deadline.Id, instance.DefinitionKey, deadline.State);
            return false;
        }

        var notifications = recipients
            .Select(userId => new Notification
            {
                InstituteId = instance.InstituteId,
                RecipientUserId = userId,
                Title = $"Reminder: {instance.DefinitionKey} / {deadline.State} due soon",
                Body = $"This work item is approaching its SLA deadline ({deadline.DueAtUtc:u}).",
                Severity = NotificationSeverity.Warning,
                CreatedAtUtc = now
            })
            .ToList();

        await _notifications.CreateBatchAsync(notifications, ct);

        foreach (var userId in recipients)
        {
            _db.Set<WorkflowReminder>().Add(new WorkflowReminder
            {
                DeadlineId = deadline.Id,
                RecipientUserId = userId,
                SentAtUtc = now,
                Channel = WorkflowReminderChannel.InApp,
                IsEscalation = false
            });
        }

        deadline.ReminderSentAtUtc = now;

        await _audit.WriteAsync(
            "Workflow.ReminderSent",
            entityType: instance.SubjectType,
            entityId: instance.SubjectId.ToString(),
            actorUserId: null, // system actor
            detailsJson: JsonSerializer.Serialize(new
            {
                deadlineId = deadline.Id,
                state = deadline.State,
                recipients = recipients.Count,
                rule.ReminderAtPercent
            }),
            cancellationToken: ct);

        return true;
    }

    private async Task<bool> EscalateInternalAsync(
        WorkflowDeadline deadline,
        WorkflowInstance instance,
        WorkflowEscalationRuleDocument rule,
        DateTime now,
        CancellationToken ct)
    {
        var recipients = await ResolveEscalationRecipientsAsync(instance, rule, ct);
        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Escalation for deadline {DeadlineId} ({Definition}/{State}) has no recipients. Marking escalated to avoid retries.",
                deadline.Id, instance.DefinitionKey, deadline.State);

            // Still mark as escalated so the sweep doesn't loop forever.
            deadline.EscalatedAtUtc = now;
            return false;
        }

        var notifications = recipients
            .Select(userId => new Notification
            {
                InstituteId = instance.InstituteId,
                RecipientUserId = userId,
                Title = $"Escalation: {instance.DefinitionKey} / {deadline.State} overdue",
                Body = $"SLA deadline ({deadline.DueAtUtc:u}) has elapsed and the work item is unresolved.",
                Severity = NotificationSeverity.Error,
                CreatedAtUtc = now
            })
            .ToList();

        await _notifications.CreateBatchAsync(notifications, ct);

        foreach (var userId in recipients)
        {
            _db.Set<WorkflowReminder>().Add(new WorkflowReminder
            {
                DeadlineId = deadline.Id,
                RecipientUserId = userId,
                SentAtUtc = now,
                Channel = WorkflowReminderChannel.InApp,
                IsEscalation = true
            });
        }

        deadline.EscalatedAtUtc = now;
        deadline.IsOverdue = true;

        await _audit.WriteAsync(
            "Workflow.Escalated",
            entityType: instance.SubjectType,
            entityId: instance.SubjectId.ToString(),
            actorUserId: null,
            detailsJson: JsonSerializer.Serialize(new
            {
                deadlineId = deadline.Id,
                state = deadline.State,
                recipients = recipients.Count,
                rule.EscalateToRole,
                rule.EscalateToPermission
            }),
            severity: AuditSeverity.Warning,
            cancellationToken: ct);

        return true;
    }

    /// <summary>
    /// Reminders go to the assignee of the still-open queue item where
    /// possible; otherwise to anyone holding the same role/permission as
    /// the escalation rule (a graceful fallback when no explicit
    /// assignee exists yet).
    /// </summary>
    private async Task<List<string>> ResolveRemindRecipientsAsync(
        WorkflowInstance instance, WorkflowDeadline deadline, CancellationToken ct)
    {
        var assignedUserId = await _db.Set<ApprovalQueue>()
            .Where(q => q.InstanceId == instance.Id && !q.IsResolved && q.AssignedToUserId != null)
            .OrderByDescending(q => q.EnqueuedAtUtc)
            .Select(q => q.AssignedToUserId!)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(assignedUserId))
        {
            return new List<string> { assignedUserId };
        }

        // No explicit assignee: defer to escalation-style resolution so
        // the message still lands somewhere meaningful.
        var rule = _registry.Get(instance.DefinitionKey)?.EscalationRules
            .FirstOrDefault(r => string.Equals(r.State, deadline.State, StringComparison.OrdinalIgnoreCase));

        return rule is null
            ? new List<string>()
            : await ResolveEscalationRecipientsAsync(instance, rule, ct);
    }

    /// <summary>
    /// Resolves escalation recipients by looking up users in
    /// <see cref="WorkflowEscalationRuleDocument.EscalateToRole"/> and
    /// filtering them to the instance's tenancy.
    ///
    /// Tenancy filter:
    /// <list type="bullet">
    ///   <item><c>Teacher</c> - intersect with TeacherProfiles in scope.</item>
    ///   <item><c>SuperAdmin</c> - cross-tenant; return all role-holders.</item>
    ///   <item>Any other role - return all role-holders (Chunk 17 will
    ///         refine when the InstituteAdmin profile lands).</item>
    /// </list>
    /// </summary>
    private async Task<List<string>> ResolveEscalationRecipientsAsync(
        WorkflowInstance instance,
        WorkflowEscalationRuleDocument rule,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rule.EscalateToRole))
        {
            return new List<string>();
        }

        var allInRole = await _userManager.GetUsersInRoleAsync(rule.EscalateToRole);
        if (allInRole.Count == 0) return new List<string>();

        // Tenant scoping for Teacher role.
        if (string.Equals(rule.EscalateToRole, Roles.Teacher, StringComparison.Ordinal)
            && instance.InstituteId.HasValue)
        {
            var instituteId = instance.InstituteId.Value;
            var inInstitute = await _db.TeacherProfiles
                .Where(t => t.InstituteId == instituteId)
                .Select(t => t.UserId)
                .ToListAsync(ct);

            var inSet = new HashSet<string>(inInstitute, StringComparer.Ordinal);
            return allInRole
                .Where(u => u.Id is not null && inSet.Contains(u.Id))
                .Select(u => u.Id!)
                .ToList();
        }

        return allInRole
            .Where(u => u.Id is not null)
            .Select(u => u.Id!)
            .ToList();
    }
}
