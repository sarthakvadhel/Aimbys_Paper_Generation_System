using System.Security.Claims;

namespace Aimbys.Application.Blueprints;

public interface IBlueprintAuthoringService
{
    Task<BlueprintResult> CreateAsync(BlueprintCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<BlueprintResult> EditAsync(Guid blueprintId, BlueprintEditRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<BlueprintResult> PublishAsync(Guid blueprintId, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<BlueprintResult> ArchiveAsync(Guid blueprintId, ClaimsPrincipal actor, CancellationToken ct = default);
}
