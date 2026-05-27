namespace Aimbys.Application.Users;

public sealed record UserInviteResult(bool Success, string? ErrorMessage = null, Guid? ProfileId = null, string? TemporaryPassword = null);
