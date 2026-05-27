namespace Aimbys.Domain.Entities;

/// <summary>
/// Tracks whether a user must change their password on next login.
/// One row per IdentityUser; absent row means no forced reset required.
/// </summary>
public class UserPasswordPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ASP.NET Identity user id.</summary>
    public string UserId { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
