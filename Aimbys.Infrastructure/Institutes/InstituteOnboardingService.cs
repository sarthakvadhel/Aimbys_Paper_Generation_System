using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Institutes;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Institutes;

/// <summary>
/// Orchestrates institute lifecycle: create, approve, reject, suspend,
/// reactivate, and subscription changes. Each method forms a single
/// unit-of-work that validates, transitions the workflow, updates the
/// entity, writes audit, enqueues domain events, and calls SaveChanges.
/// </summary>
public class InstituteOnboardingService : IInstituteOnboardingService
{
    private const string DefinitionKey = "InstituteApproval";
    private const string SubjectType = "Institute";

    private readonly AppDbContext _db;
    private readonly IWorkflowService _workflow;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly UserManager<IdentityUser> _userManager;

    public InstituteOnboardingService(
        AppDbContext db,
        IWorkflowService workflow,
        IAuditWriter audit,
        DomainEventCollector events,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _workflow = workflow;
        _audit = audit;
        _events = events;
        _userManager = userManager;
    }

    public async Task<WorkflowResult> CreateAsync(InstituteCreateRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return WorkflowResult.Failure("VALIDATION", "Institute name is required.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return WorkflowResult.Failure("VALIDATION", "Institute code is required.");
        if (string.IsNullOrWhiteSpace(request.AdminEmail))
            return WorkflowResult.Failure("VALIDATION", "Admin email is required.");

        var existingCode = await _db.Institutes
            .IgnoreQueryFilters()
            .AnyAsync(i => i.Code == request.Code, ct);
        if (existingCode)
            return WorkflowResult.Failure("DUPLICATE_CODE", $"Institute code '{request.Code}' is already in use.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        // Create the Institute entity
        var institute = new Institute
        {
            Name = request.Name,
            Code = request.Code,
            Type = request.Type,
            City = request.City,
            State = request.State,
            Country = request.Country,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            LicenseTier = request.LicenseTier,
            Status = InstituteStatus.PendingApproval,
            SubscriptionStatus = InstituteSubscriptionStatus.Trial
        };
        _db.Institutes.Add(institute);

        // Create IdentityUser for the admin
        var adminUser = new IdentityUser
        {
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            EmailConfirmed = true
        };
        var createResult = await _userManager.CreateAsync(adminUser, "Admin@1234");
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return WorkflowResult.Failure("USER_CREATE_FAILED", $"Failed to create admin user: {errors}");
        }
        await _userManager.AddToRoleAsync(adminUser, Roles.InstituteAdmin);

        // Create TeacherProfile placeholder
        var displayName = request.AdminEmail.Split('@')[0];
        var teacherProfile = new TeacherProfile
        {
            UserId = adminUser.Id,
            InstituteId = institute.Id,
            DisplayName = displayName,
            Status = ProfileStatus.Active
        };
        _db.TeacherProfiles.Add(teacherProfile);

        // Start the InstituteApproval workflow
        var workflowResult = await _workflow.StartAsync(
            DefinitionKey,
            SubjectType,
            institute.Id,
            actorUserId,
            instituteId: null,
            cancellationToken: ct);

        if (!workflowResult.IsSuccess)
            return workflowResult;

        // Audit
        await _audit.WriteAsync(
            "Institute.Created",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name, institute.Code, AdminEmail = request.AdminEmail }),
            cancellationToken: ct);

        // Domain event
        _events.Enqueue(new WorkflowTransitionedEvent
        {
            InstituteId = institute.Id,
            InstanceId = workflowResult.InstanceId ?? Guid.Empty,
            DefinitionKey = DefinitionKey,
            SubjectType = SubjectType,
            SubjectId = institute.Id,
            FromState = string.Empty,
            ToState = "PendingApproval",
            ActorUserId = actorUserId
        });

        await _db.SaveChangesAsync(ct);

        return workflowResult;
    }

    public async Task<WorkflowResult> ApproveAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct);
        if (institute is null)
            return WorkflowResult.Failure("NOT_FOUND", "Institute not found.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        // Get the workflow instance
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == SubjectType && wi.SubjectId == instituteId && !wi.IsCompleted, ct);
        if (instance is null)
            return WorkflowResult.Failure("NO_WORKFLOW", "No active workflow found for this institute.");

        var result = await _workflow.TransitionAsync(instance.Id, "Active", actor, comment: null, ct);
        if (!result.IsSuccess)
            return result;

        // Update entity
        institute.Status = InstituteStatus.Active;
        institute.ApprovedByUserId = actorUserId;
        institute.ApprovedAtUtc = DateTime.UtcNow;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        // Audit
        await _audit.WriteAsync(
            "Institute.Approved",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name }),
            cancellationToken: ct);

        // Domain event
        _events.Enqueue(new InstituteApprovedEvent
        {
            InstituteId = institute.Id,
            InstituteName = institute.Name,
            ApprovedByUserId = actorUserId,
            InstituteAdminUserId = actorUserId
        });

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<WorkflowResult> RejectAsync(Guid instituteId, ClaimsPrincipal actor, string? reason, CancellationToken ct)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct);
        if (institute is null)
            return WorkflowResult.Failure("NOT_FOUND", "Institute not found.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == SubjectType && wi.SubjectId == instituteId && !wi.IsCompleted, ct);
        if (instance is null)
            return WorkflowResult.Failure("NO_WORKFLOW", "No active workflow found for this institute.");

        var result = await _workflow.TransitionAsync(instance.Id, "Rejected", actor, comment: reason, ct);
        if (!result.IsSuccess)
            return result;

        institute.Status = InstituteStatus.Rejected;
        institute.ApprovedByUserId = actorUserId;
        institute.ApprovedAtUtc = DateTime.UtcNow;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Institute.Rejected",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name, Reason = reason }),
            cancellationToken: ct);

        _events.Enqueue(new WorkflowTransitionedEvent
        {
            InstituteId = institute.Id,
            InstanceId = instance.Id,
            DefinitionKey = DefinitionKey,
            SubjectType = SubjectType,
            SubjectId = instituteId,
            FromState = "PendingApproval",
            ToState = "Rejected",
            ActorUserId = actorUserId,
            Comment = reason
        });

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<WorkflowResult> SuspendAsync(Guid instituteId, ClaimsPrincipal actor, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return WorkflowResult.Failure("VALIDATION", "Reason is required for suspension.");

        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct);
        if (institute is null)
            return WorkflowResult.Failure("NOT_FOUND", "Institute not found.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == SubjectType && wi.SubjectId == instituteId && !wi.IsCompleted, ct);
        if (instance is null)
            return WorkflowResult.Failure("NO_WORKFLOW", "No active workflow found for this institute.");

        var result = await _workflow.TransitionAsync(instance.Id, "Suspended", actor, comment: reason, ct);
        if (!result.IsSuccess)
            return result;

        institute.Status = InstituteStatus.Suspended;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Institute.Suspended",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name, Reason = reason }),
            cancellationToken: ct);

        _events.Enqueue(new InstituteSuspendedEvent
        {
            InstituteId = institute.Id,
            InstituteName = institute.Name,
            Reason = reason,
            ActorUserId = actorUserId
        });

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<WorkflowResult> ReactivateAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct);
        if (institute is null)
            return WorkflowResult.Failure("NOT_FOUND", "Institute not found.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == SubjectType && wi.SubjectId == instituteId && !wi.IsCompleted, ct);
        if (instance is null)
            return WorkflowResult.Failure("NO_WORKFLOW", "No active workflow found for this institute.");

        var result = await _workflow.TransitionAsync(instance.Id, "Reactivated", actor, comment: null, ct);
        if (!result.IsSuccess)
            return result;

        institute.Status = InstituteStatus.Active;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Institute.Reactivated",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name }),
            cancellationToken: ct);

        _events.Enqueue(new WorkflowTransitionedEvent
        {
            InstituteId = institute.Id,
            InstanceId = instance.Id,
            DefinitionKey = DefinitionKey,
            SubjectType = SubjectType,
            SubjectId = instituteId,
            FromState = "Suspended",
            ToState = "Reactivated",
            ActorUserId = actorUserId
        });

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<WorkflowResult> ChangeSubscriptionAsync(Guid instituteId, InstituteSubscriptionStatus newStatus, DateTime? expiresAtUtc, ClaimsPrincipal actor, CancellationToken ct)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == instituteId, ct);
        if (institute is null)
            return WorkflowResult.Failure("NOT_FOUND", "Institute not found.");

        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var oldStatus = institute.SubscriptionStatus;

        institute.SubscriptionStatus = newStatus;
        institute.SubscriptionExpiresAtUtc = expiresAtUtc;
        institute.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Institute.SubscriptionChanged",
            "Institute",
            institute.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { institute.Name, OldStatus = oldStatus.ToString(), NewStatus = newStatus.ToString(), ExpiresAtUtc = expiresAtUtc }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        // Return a synthetic success (no workflow transition for subscription changes)
        return WorkflowResult.Success(Guid.Empty, newStatus.ToString());
    }
}
