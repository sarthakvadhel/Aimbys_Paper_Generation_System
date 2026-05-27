using System.Text.RegularExpressions;
using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Notifications;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="INotificationTemplateService"/>. Resolves a
/// template (with translation chain) and renders it through a small
/// <c>{placeholder}</c> interpolator into a ready-to-persist
/// <see cref="Notification"/>.
///
/// <para>
/// Reads are cached in <see cref="IMemoryCache"/> with a 5-minute
/// sliding TTL. Templates rarely change at runtime, so the cache
/// hit-rate approaches 100%; callers that mutate templates should
/// call <see cref="InvalidateCache"/> (idempotent).
/// </para>
/// </summary>
public sealed class NotificationTemplateService : INotificationTemplateService
{
    /// <summary>BCP-47 fallback language. Translation chain ends here before defaulting to template columns.</summary>
    public const string FallbackLanguage = "en-IN";

    /// <summary>Sliding cache window. Public so tests can verify the contract.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private const string CacheKeyPrefix = "notif-template:";

    /// <summary>Compiled once: matches <c>{name}</c> with letters/digits/dot/underscore.</summary>
    private static readonly Regex PlaceholderRegex =
        new(@"\{([A-Za-z_][A-Za-z0-9_\.]*)\}", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        AppDbContext db,
        IMemoryCache cache,
        ILogger<NotificationTemplateService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Notification?> RenderAsync(
        string templateKey,
        string recipientUserId,
        IReadOnlyDictionary<string, string?> placeholders,
        Guid? instituteId = null,
        string? languageCode = null,
        NotificationSeverity? severityOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
            throw new ArgumentException("templateKey is required.", nameof(templateKey));

        var snapshot = await LoadAsync(templateKey, cancellationToken);

        if (snapshot is null)
        {
            throw new KeyNotFoundException(
                $"NotificationTemplate '{templateKey}' is not registered.");
        }

        if (!snapshot.IsActive)
        {
            // Disabled but present: opt-out by returning null so the
            // projection can skip the recipient cleanly.
            return null;
        }

        // Translation chain: requested → en-IN → template default.
        var (titleTemplate, bodyTemplate) = ResolveLanguageVariant(snapshot, languageCode);

        return new Notification
        {
            InstituteId = instituteId,
            RecipientUserId = recipientUserId,
            Title = Interpolate(titleTemplate, placeholders),
            Body = bodyTemplate is null ? null : Interpolate(bodyTemplate, placeholders),
            Severity = severityOverride ?? snapshot.DefaultSeverity,
            RouteUrl = string.IsNullOrEmpty(snapshot.DefaultRoutePattern)
                ? null
                : Interpolate(snapshot.DefaultRoutePattern, placeholders),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Drops the cached snapshot for one template. Called by admin
    /// tooling after a template / translation row is updated.
    /// </summary>
    public void InvalidateCache(string templateKey)
    {
        if (!string.IsNullOrEmpty(templateKey))
        {
            _cache.Remove(CacheKeyPrefix + templateKey);
        }
    }

    // ----- helpers ------------------------------------------------------

    /// <summary>
    /// Loads the template + translations and projects them into an
    /// in-memory snapshot. Cached so repeated renders don't round-trip.
    /// </summary>
    private async Task<TemplateSnapshot?> LoadAsync(string templateKey, CancellationToken ct)
    {
        var cacheKey = CacheKeyPrefix + templateKey;
        if (_cache.TryGetValue<TemplateSnapshot>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var template = await _db.Set<NotificationTemplate>()
            .AsNoTracking()
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Key == templateKey, ct);

        if (template is null)
        {
            return null;
        }

        var translations = template.Translations
            .ToDictionary(
                t => t.LanguageCode,
                t => new TranslationSnapshot(t.TitleTemplate, t.BodyTemplate),
                StringComparer.OrdinalIgnoreCase);

        var snapshot = new TemplateSnapshot(
            template.Key,
            template.IsActive,
            template.DefaultSeverity,
            template.TitleTemplate,
            template.BodyTemplate,
            template.DefaultRoutePattern,
            translations);

        _cache.Set(cacheKey, snapshot, CacheTtl);
        return snapshot;
    }

    /// <summary>
    /// Picks the right <c>(title, body)</c> pair for the requested
    /// language, walking the chain
    /// <c>requested → en-IN → template default</c>.
    /// </summary>
    private static (string Title, string? Body) ResolveLanguageVariant(
        TemplateSnapshot snapshot,
        string? languageCode)
    {
        if (!string.IsNullOrEmpty(languageCode)
            && snapshot.Translations.TryGetValue(languageCode, out var requested))
        {
            return (requested.Title, requested.Body);
        }

        if (snapshot.Translations.TryGetValue(FallbackLanguage, out var fallback))
        {
            return (fallback.Title, fallback.Body);
        }

        return (snapshot.DefaultTitle, snapshot.DefaultBody);
    }

    /// <summary>
    /// Replaces every <c>{name}</c> token in <paramref name="template"/>
    /// with the matching value from <paramref name="placeholders"/>.
    /// Unmatched placeholders are left untouched and a warning is
    /// logged &mdash; a missing value is better than a missing
    /// notification.
    /// </summary>
    private string Interpolate(string template, IReadOnlyDictionary<string, string?> placeholders)
    {
        if (string.IsNullOrEmpty(template)) return template;

        return PlaceholderRegex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (placeholders.TryGetValue(name, out var value))
            {
                return value ?? string.Empty;
            }

            _logger.LogWarning(
                "NotificationTemplate placeholder '{Placeholder}' has no value; leaving raw token in output.",
                name);
            return match.Value;
        });
    }

    /// <summary>Lightweight in-cache projection of one template + translations.</summary>
    private sealed record TemplateSnapshot(
        string Key,
        bool IsActive,
        NotificationSeverity DefaultSeverity,
        string DefaultTitle,
        string? DefaultBody,
        string? DefaultRoutePattern,
        IReadOnlyDictionary<string, TranslationSnapshot> Translations);

    private sealed record TranslationSnapshot(string Title, string? Body);
}
