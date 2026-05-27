using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Retention;

/// <summary>
/// What happens once a row of <see cref="EntityType"/> passes its
/// retention window. Paired with
/// <see cref="RetentionPolicy"/> on the same
/// <see cref="EntityType"/>; if no archive policy exists the handler
/// defaults to <see cref="ArchiveStrategy.SoftArchive"/> (the
/// safest option).
/// </summary>
public class ArchivePolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Entity type discriminator (matches <c>RetentionPolicy.EntityType</c>). Globally unique.</summary>
    public string EntityType { get; set; } = string.Empty;

    public ArchiveStrategy Strategy { get; set; } = ArchiveStrategy.SoftArchive;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
