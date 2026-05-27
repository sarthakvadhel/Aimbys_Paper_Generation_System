namespace Aimbys.Infrastructure.Identity;

/// <summary>
/// Optional configuration used by <see cref="IdentitySeeder"/> to provision a
/// first admin user during startup. Both <see cref="Email"/> and
/// <see cref="Password"/> must be supplied for the seed to run.
/// </summary>
public class DefaultAdminOptions
{
    /// <summary>Configuration section name: <c>Identity:DefaultAdmin</c>.</summary>
    public const string SectionName = "Identity:DefaultAdmin";

    public string? Email { get; set; }
    public string? Password { get; set; }
}
