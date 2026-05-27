namespace Aimbys.Application.Users;

public sealed record UserPageResult(
    IReadOnlyList<UserListItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed record UserListItem(
    Guid ProfileId,
    string UserId,
    string DisplayName,
    string Email,
    string Role,
    string? Department,
    string Status,
    bool IsTeacher);
