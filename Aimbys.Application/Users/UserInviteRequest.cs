namespace Aimbys.Application.Users;

public sealed record UserInviteRequest(
    Guid InstituteId,
    string DisplayName,
    string Email,
    string Role, // "Teacher" | "Student" | "InstituteAdmin"
    Guid? DepartmentId,
    Guid? ClassBatchId);
