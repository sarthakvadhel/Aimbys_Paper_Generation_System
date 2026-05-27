using Aimbys.Application.Multilingual;
using Aimbys.Domain.Entities.Multilingual;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Multilingual;

/// <summary>
/// Infrastructure implementation of <see cref="IMultilingualService"/>.
/// Handles translation retrieval, persistence, language resolution, and
/// basic HTML sanitization.
/// </summary>
public sealed class MultilingualService : IMultilingualService
{
    private readonly AppDbContext _db;

    public MultilingualService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<QuestionTranslationView?> GetTranslationAsync(
        Guid questionVersionId, string languageCode, CancellationToken ct = default)
    {
        var translation = await _db.QuestionTranslations
            .Join(_db.Languages,
                qt => qt.LanguageId,
                l => l.Id,
                (qt, l) => new { qt, l })
            .Where(x => x.qt.QuestionVersionId == questionVersionId
                     && x.l.Code == languageCode)
            .Select(x => new QuestionTranslationView(
                x.qt.Id,
                x.qt.BodyHtml,
                x.qt.InstructionsHtml,
                x.qt.OptionsJson,
                x.l.Code,
                x.qt.Status))
            .FirstOrDefaultAsync(ct);

        return translation;
    }

    /// <inheritdoc />
    public async Task<bool> SaveTranslationAsync(
        Guid questionVersionId,
        Guid languageId,
        string bodyHtml,
        string? instructionsHtml,
        string? optionsJson,
        string translatorUserId,
        CancellationToken ct = default)
    {
        var sanitizedBody = HtmlSanitizer.Sanitize(bodyHtml);
        var sanitizedInstructions = instructionsHtml is not null
            ? HtmlSanitizer.Sanitize(instructionsHtml)
            : null;

        var existing = await _db.QuestionTranslations
            .FirstOrDefaultAsync(qt =>
                qt.QuestionVersionId == questionVersionId
                && qt.LanguageId == languageId, ct);

        if (existing is not null)
        {
            existing.BodyHtml = sanitizedBody;
            existing.InstructionsHtml = sanitizedInstructions;
            existing.OptionsJson = optionsJson;
            existing.TranslatorUserId = translatorUserId;
            existing.Status = TranslationStatus.Draft;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var translation = new QuestionTranslation
            {
                QuestionVersionId = questionVersionId,
                LanguageId = languageId,
                BodyHtml = sanitizedBody,
                InstructionsHtml = sanitizedInstructions,
                OptionsJson = optionsJson,
                TranslatorUserId = translatorUserId,
                Status = TranslationStatus.Draft
            };
            _db.QuestionTranslations.Add(translation);
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageView>> GetAvailableLanguagesAsync(
        Guid paperVersionId, CancellationToken ct = default)
    {
        var languages = await _db.PaperLanguageSets
            .Where(pls => pls.PaperVersionId == paperVersionId)
            .Join(_db.Languages,
                pls => pls.LanguageId,
                l => l.Id,
                (pls, l) => new LanguageView(l.Id, l.Code, l.Name, pls.IsDefault))
            .ToListAsync(ct);

        return languages;
    }

    /// <inheritdoc />
    public async Task<string> ResolveLanguageForStudentAsync(
        Guid studentProfileId, Guid paperVersionId, CancellationToken ct = default)
    {
        var student = await _db.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        if (student?.PreferredLanguageId is not null)
        {
            // Check if the paper supports the student's preferred language
            var preferredMatch = await _db.PaperLanguageSets
                .Where(pls => pls.PaperVersionId == paperVersionId
                           && pls.LanguageId == student.PreferredLanguageId.Value)
                .Join(_db.Languages,
                    pls => pls.LanguageId,
                    l => l.Id,
                    (pls, l) => l.Code)
                .FirstOrDefaultAsync(ct);

            if (preferredMatch is not null)
                return preferredMatch;
        }

        // Fall back to paper's default language
        var defaultCode = await _db.PaperLanguageSets
            .Where(pls => pls.PaperVersionId == paperVersionId && pls.IsDefault)
            .Join(_db.Languages,
                pls => pls.LanguageId,
                l => l.Id,
                (pls, l) => l.Code)
            .FirstOrDefaultAsync(ct);

        return defaultCode ?? "en";
    }
}
