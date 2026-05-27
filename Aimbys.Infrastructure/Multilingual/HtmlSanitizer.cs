using System.Text.RegularExpressions;

namespace Aimbys.Infrastructure.Multilingual;

/// <summary>
/// Lightweight HTML sanitizer that strips script tags and event handler
/// attributes. Production deployments should replace with a mature library.
/// </summary>
internal static partial class HtmlSanitizer
{
    public static string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove <script>...</script> including contents
        var result = ScriptTagRegex().Replace(html, string.Empty);

        // Remove on* event-handler attributes (onclick, onerror, etc.)
        result = EventHandlerRegex().Replace(result, string.Empty);

        return result.Trim();
    }

    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();
}
