using System.Security.Claims;

namespace Aimbys.Application.Results;

public interface IResultPublicationService
{
    Task<(bool CanPublish, string? BlockingReason)> CanPublishAsync(Guid examId, CancellationToken ct = default);
    Task<ResultPublishResult> PublishAsync(Guid examId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<StudentResultView?> GetStudentResultAsync(Guid attemptId, string studentUserId, CancellationToken ct = default);
}
