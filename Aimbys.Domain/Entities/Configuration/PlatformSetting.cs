namespace Aimbys.Domain.Entities.Configuration;

/// <summary>
/// Platform-wide configuration value. Setting/reading is mediated by
/// <c>IConfigurationService</c> so reads come out of the in-memory
/// cache and writes invalidate that cache.
///
/// <para>
/// <see cref="ValueJson"/> stores the value as JSON so the same row
/// type can carry strings, numbers, booleans, or compound objects
/// without bespoke columns. Consumers deserialize with the type they
/// expect; mismatches surface as
/// <see cref="System.Text.Json.JsonException"/> at read time.
/// </para>
/// </summary>
public class PlatformSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable, dot-separated key (e.g. <c>"branding.support_email"</c>). Globally unique.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>JSON-encoded value. Never null; an "unset" key has no row.</summary>
    public string ValueJson { get; set; } = "null";

    /// <summary>Free-form description shown in admin tooling.</summary>
    public string? Description { get; set; }

    /// <summary>Identity user id of the most recent writer.</summary>
    public string? UpdatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
