namespace Aimbys.Application.Users;

public sealed record UserPageFilter(
    Guid InstituteId,
    string? SearchQuery,
    string? RoleFilter,
    string? StatusFilter,
    int Page = 1,
    int PageSize = 20);
