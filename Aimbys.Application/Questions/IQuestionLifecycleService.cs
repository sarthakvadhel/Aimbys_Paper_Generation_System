using System.Security.Claims;

namespace Aimbys.Application.Questions;

public interface IQuestionLifecycleService
{
    Task<QuestionLifecycleResult> SubmitForReviewAsync(Guid questionId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<QuestionLifecycleResult> AssignReviewerAsync(Guid questionId, Guid reviewerProfileId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<QuestionLifecycleResult> ApproveAsync(Guid questionId, ClaimsPrincipal actor, string? comment = null, CancellationToken ct = default);
    Task<QuestionLifecycleResult> RejectAsync(Guid questionId, ClaimsPrincipal actor, string comment, CancellationToken ct = default);
    Task<QuestionLifecycleResult> RetireAsync(Guid questionId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<QuestionLifecycleResult> AutoAssignReviewerAsync(Guid questionId, CancellationToken ct = default);
}
