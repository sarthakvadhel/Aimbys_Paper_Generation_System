namespace Aimbys.Application.Moderation;

public sealed record ModerationResult(bool Success, string? Error = null)
{
    public static ModerationResult Ok() => new(true);
    public static ModerationResult Fail(string error) => new(false, error);
}
