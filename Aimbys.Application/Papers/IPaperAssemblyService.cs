using System.Security.Claims;

namespace Aimbys.Application.Papers;

/// <summary>
/// End-to-end paper assembly + lifecycle service. Every method routes
/// state changes through <c>IWorkflowService</c> &mdash; controllers
/// must <em>never</em> mutate <c>Paper.Status</c> directly. State
/// transitions:
///
/// <list type="bullet">
///   <item><c>Draft → SubmittedForApproval</c> (author submits)</item>
///   <item><c>SubmittedForApproval → Approved</c> (institute admin approves)</item>
///   <item><c>SubmittedForApproval → Returned</c> (institute admin returns with comment)</item>
///   <item><c>Returned → SubmittedForApproval</c> (author resubmits after edits)</item>
///   <item><c>Approved → Published</c> (institute admin makes available for scheduling)</item>
///   <item><c>Published → Archived</c> (institute admin retires the paper)</item>
/// </list>
/// </summary>
public interface IPaperAssemblyService
{
    Task<PaperResult> CreateDraftAsync(PaperCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> SaveDraftAsync(Guid paperId, PaperSaveRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> GenerateFromBlueprintAsync(Guid paperId, Guid blueprintVersionId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> SubmitForApprovalAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> ApproveAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> ReturnAsync(Guid paperId, ClaimsPrincipal actor, string comment, CancellationToken ct = default);

    /// <summary>
    /// Publish an approved paper, making it eligible for exam scheduling.
    /// Restricted to InstituteAdmin (enforced by the workflow definition).
    /// </summary>
    Task<PaperResult> PublishAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);

    /// <summary>
    /// Archive a published paper. Existing exam attempts and results are
    /// preserved; the paper becomes unavailable for new schedules.
    /// Restricted to InstituteAdmin.
    /// </summary>
    Task<PaperResult> ArchiveAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);
}
