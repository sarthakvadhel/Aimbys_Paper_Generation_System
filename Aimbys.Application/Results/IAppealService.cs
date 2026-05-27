using System.Security.Claims;

namespace Aimbys.Application.Results;

public interface IAppealService
{
    Task<AppealResult> FileAppealAsync(Guid attemptAnswerId, string studentUserId, string reason, CancellationToken ct = default);
    Task<AppealResult> UpholdAsync(Guid appealId, ClaimsPrincipal actor, string? comment, CancellationToken ct = default);
    Task<AppealResult> AdjustAsync(Guid appealId, ClaimsPrincipal actor, decimal newScore, string reason, CancellationToken ct = default);
}
