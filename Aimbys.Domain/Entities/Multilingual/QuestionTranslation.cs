using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Multilingual;

public class QuestionTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionVersionId { get; set; }
    public Guid LanguageId { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public string? InstructionsHtml { get; set; }
    public string? OptionsJson { get; set; } // JSON array matching QuestionOption structure
    public TranslationStatus Status { get; set; } = TranslationStatus.Draft;
    public string TranslatorUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
