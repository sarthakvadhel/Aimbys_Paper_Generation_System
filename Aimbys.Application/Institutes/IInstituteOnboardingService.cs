using System.Security.Claims;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.Institutes;

/// <summary>
/// Orchestrates institute lifecycle operations: onboarding, approval,
/// suspension, reactivation, and subscription changes. Each method
/// validates, transitions the workflow, writes audit, enqueues domain
/// events, and persists in a single unit-of-work.
/// </summary>
public interface IInstituteOnboardingService
{
    Task<WorkflowResult> CreateAsync(InstituteCreateRequest request, ClaimsPrincipal actor, CancellationToken ct);
    Task<WorkflowResult> ApproveAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct);
    Task<WorkflowResult> RejectAsync(Guid instituteId, ClaimsPrincipal actor, string? reason, CancellationToken ct);
    Task<WorkflowResult> SuspendAsync(Guid instituteId, ClaimsPrincipal actor, string reason, CancellationToken ct);
    Task<WorkflowResult> ReactivateAsync(Guid instituteId, ClaimsPrincipal actor, CancellationToken ct);
    Task<WorkflowResult> ChangeSubscriptionAsync(Guid instituteId, InstituteSubscriptionStatus newStatus, DateTime? expiresAtUtc, ClaimsPrincipal actor, CancellationToken ct);
}
