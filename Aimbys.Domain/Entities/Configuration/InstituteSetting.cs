namespace Aimbys.Domain.Entities.Configuration;

/// <summary>
/// Per-institute configuration value. Same JSON-encoded shape as
/// <see cref="PlatformSetting"/>, scoped by <see cref="InstituteId"/>.
/// (Institute, Key) is unique &mdash; the platform never holds two
/// rows for the same key on the same tenant.
/// </summary>
public class InstituteSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstituteId { get; set; }

    /// <summary>Stable, dot-separated key. Unique within the institute.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>JSON-encoded value. Never null; an "unset" key has no row.</summary>
    public string ValueJson { get; set; } = "null";

    public string? Description { get; set; }

    public string? UpdatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Institute? Institute { get; set; }
}
