namespace Aimbys.Domain.Entities.Multilingual;

public class PaperLanguageSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaperVersionId { get; set; }
    public Guid LanguageId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
