namespace Aimbys.Application.Multilingual;

public interface IMultilingualService
{
    Task<QuestionTranslationView?> GetTranslationAsync(Guid questionVersionId, string languageCode, CancellationToken ct = default);
    Task<bool> SaveTranslationAsync(Guid questionVersionId, Guid languageId, string bodyHtml, string? instructionsHtml, string? optionsJson, string translatorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<LanguageView>> GetAvailableLanguagesAsync(Guid paperVersionId, CancellationToken ct = default);
    Task<string> ResolveLanguageForStudentAsync(Guid studentProfileId, Guid paperVersionId, CancellationToken ct = default);
}
