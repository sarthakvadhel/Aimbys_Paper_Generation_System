using System.Text.RegularExpressions;

namespace Aimbys.Infrastructure.Questions;

public static class HtmlSanitizer
{
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        // Strip <script> tags and event handlers
        var result = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*""[^""]*""", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*on\w+\s*=\s*'[^']*'", "", RegexOptions.IgnoreCase);
        return result;
    }
}
