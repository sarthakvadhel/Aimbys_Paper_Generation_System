using System.Security.Claims;

namespace Aimbys.Application.Papers;

public interface IPaperAssemblyService
{
    Task<PaperResult> CreateDraftAsync(PaperCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> SaveDraftAsync(Guid paperId, PaperSaveRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> GenerateFromBlueprintAsync(Guid paperId, Guid blueprintVersionId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> SubmitForApprovalAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> ApproveAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<PaperResult> ReturnAsync(Guid paperId, ClaimsPrincipal actor, string comment, CancellationToken ct = default);
}
