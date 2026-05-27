using System.Security.Claims;

namespace Aimbys.Application.Moderation;

public interface IModerationService
{
    Task<ModerationResult> EnqueueForModerationAsync(Guid evaluationId, CancellationToken ct = default);
    Task<ModerationResult> ApproveAsync(Guid moderationId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<ModerationResult> RequireChangesAsync(Guid moderationId, ClaimsPrincipal actor, string comment, CancellationToken ct = default);
    Task<ModerationResult> OverrideAsync(Guid moderationId, ClaimsPrincipal actor, decimal newScore, decimal maxScore, string reason, CancellationToken ct = default);
    Task<ModerationContext?> GetContextAsync(Guid moderationId, CancellationToken ct = default);
}
