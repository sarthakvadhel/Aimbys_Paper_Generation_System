using System.Security.Claims;

namespace Aimbys.Application.Questions;

public interface IQuestionAuthoringService
{
    Task<QuestionCreateResult> CreateAsync(QuestionCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<QuestionEditResult> EditAsync(Guid questionId, QuestionEditRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionVersionSummary>> GetRevisionHistoryAsync(Guid questionId, CancellationToken ct = default);
}
