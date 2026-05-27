namespace Aimbys.Domain.Entities.Broadcasts;

/// <summary>
/// Platform-wide message scheduled by SuperAdmin and rendered as a
/// dismissible banner for the target audience during the active window.
/// </summary>
public class Broadcast
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>
    /// JSON describing audience filter, e.g.:
    /// {"institutes":["all"],"roles":["Teacher","Student"]}
    /// </summary>
    public string AudienceFilterJson { get; set; } = "{}";

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
}
