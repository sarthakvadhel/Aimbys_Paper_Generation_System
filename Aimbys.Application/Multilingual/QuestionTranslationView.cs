using Aimbys.Domain.Enums;

namespace Aimbys.Application.Multilingual;

public record QuestionTranslationView(
    Guid Id,
    string BodyHtml,
    string? InstructionsHtml,
    string? OptionsJson,
    string LanguageCode,
    TranslationStatus Status);
