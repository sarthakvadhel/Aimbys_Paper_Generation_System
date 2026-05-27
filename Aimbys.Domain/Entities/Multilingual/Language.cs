namespace Aimbys.Domain.Entities.Multilingual;

public class Language
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "en-IN", "hi-IN"
    public string Name { get; set; } = string.Empty; // e.g. "English (India)", "Hindi"
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
