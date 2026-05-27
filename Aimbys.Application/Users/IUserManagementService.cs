using System.Security.Claims;

namespace Aimbys.Application.Users;

public interface IUserManagementService
{
    Task<UserInviteResult> InviteAsync(UserInviteRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<bool> UpdateProfileAsync(Guid profileId, UserProfileUpdateRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<bool> SuspendAsync(Guid profileId, string? reason, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<bool> ReactivateAsync(Guid profileId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<UserPageResult> GetPageAsync(UserPageFilter filter, CancellationToken ct = default);
}
