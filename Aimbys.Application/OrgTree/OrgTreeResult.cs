namespace Aimbys.Application.OrgTree;

public sealed record OrgTreeResult(bool Success, string? ErrorMessage = null, Guid? EntityId = null);
