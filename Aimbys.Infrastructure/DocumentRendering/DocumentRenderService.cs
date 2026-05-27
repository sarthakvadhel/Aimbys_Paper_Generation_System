using System.Net;
using System.Text;
using Aimbys.Application.DocumentRendering;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.DocumentRendering;

/// <summary>
/// Default <see cref="IDocumentRenderService"/>. V1 emits simple HTML
/// "placeholder" documents for each surface so the rest of the
/// platform (controllers, downloads, scheduled exports) can be wired
/// up before the underlying entities (Paper, Exam, Result, Transcript)
/// are introduced in later chunks.
///
/// <para>
/// Each render method:
/// </para>
/// <list type="number">
///   <item>Builds a tiny self-contained HTML document.</item>
///   <item>For <c>DocumentFormat.Html</c> &mdash; returns it directly.</item>
///   <item>For <c>DocumentFormat.Pdf</c> &mdash; pipes the HTML
///         through <see cref="IHtmlToPdfConverter"/>. The V1 logging
///         stub returns the HTML bytes; production replaces the
///         converter without touching this class.</item>
/// </list>
/// </summary>
public sealed class DocumentRenderService : IDocumentRenderService
{
    private readonly IHtmlToPdfConverter _pdfConverter;
    private readonly ILogger<DocumentRenderService> _logger;

    public DocumentRenderService(
        IHtmlToPdfConverter pdfConverter,
        ILogger<DocumentRenderService> logger)
    {
        _pdfConverter = pdfConverter;
        _logger = logger;
    }

    public Task<RenderedDocument> RenderPaperAsync(
        Guid paperId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default)
    {
        var html = BuildPlaceholder(
            title: "Question Paper",
            heading: "Question Paper",
            keyValue: ("PaperId", paperId.ToString()),
            note: "Question content is generated in a later chunk; this is the V1 placeholder.");

        return RenderAsync(html, $"paper-{paperId}", format, cancellationToken);
    }

    public Task<RenderedDocument> RenderResultSheetAsync(
        Guid attemptId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default)
    {
        var html = BuildPlaceholder(
            title: "Result Sheet",
            heading: "Exam Result Sheet",
            keyValue: ("AttemptId", attemptId.ToString()),
            note: "Per-question scoring is rendered when evaluation lands in a later chunk.");

        return RenderAsync(html, $"result-{attemptId}", format, cancellationToken);
    }

    public Task<RenderedDocument> RenderCertificateAsync(
        Guid studentId,
        Guid examId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default)
    {
        var html = BuildPlaceholder(
            title: "Certificate",
            heading: "Certificate of Achievement",
            keyValue: ("Student", studentId.ToString()),
            secondary: ("Exam", examId.ToString()),
            note: "Real certificate copy and seal arrive when the certificate templates land.");

        return RenderAsync(html, $"certificate-{studentId}-{examId}", format, cancellationToken);
    }

    public Task<RenderedDocument> RenderTranscriptAsync(
        Guid studentId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default)
    {
        var html = BuildPlaceholder(
            title: "Transcript",
            heading: "Cumulative Transcript",
            keyValue: ("Student", studentId.ToString()),
            note: "Aggregated grades will populate once the analytics chunk lands.");

        return RenderAsync(html, $"transcript-{studentId}", format, cancellationToken);
    }

    public Task<RenderedDocument> RenderAnalyticsExportAsync(
        string scope,
        IReadOnlyDictionary<string, string> filters,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append("<table><thead><tr><th>Filter</th><th>Value</th></tr></thead><tbody>");
        foreach (var kv in filters)
        {
            sb.Append("<tr><td>")
              .Append(WebUtility.HtmlEncode(kv.Key))
              .Append("</td><td>")
              .Append(WebUtility.HtmlEncode(kv.Value))
              .Append("</td></tr>");
        }
        sb.Append("</tbody></table>");

        var html = BuildPlaceholder(
            title: $"Analytics Export — {scope}",
            heading: $"Analytics Export: {WebUtility.HtmlEncode(scope)}",
            keyValue: ("Scope", scope),
            note: "Aggregations are rendered when the analytics service lands.",
            extraBodyHtml: sb.ToString());

        return RenderAsync(html, $"analytics-{scope}", format, cancellationToken);
    }

    // ----- helpers ------------------------------------------------------

    private async Task<RenderedDocument> RenderAsync(
        string html,
        string fileSlug,
        DocumentFormat format,
        CancellationToken cancellationToken)
    {
        if (format == DocumentFormat.Html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            return new RenderedDocument(
                Content: new MemoryStream(bytes, writable: false),
                FileName: $"{fileSlug}.html",
                ContentType: "text/html; charset=utf-8");
        }

        var pdfStream = await _pdfConverter.ConvertAsync(html, cancellationToken);
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        _logger.LogInformation(
            "DocumentRenderService produced PDF output for {Slug}.", fileSlug);

        return new RenderedDocument(
            Content: pdfStream,
            FileName: $"{fileSlug}.pdf",
            ContentType: "application/pdf");
    }

    private static string BuildPlaceholder(
        string title,
        string heading,
        (string Key, string Value) keyValue,
        string note,
        (string Key, string Value)? secondary = null,
        string? extraBodyHtml = null)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>")
          .Append(WebUtility.HtmlEncode(title))
          .Append("</title>")
          .Append("<style>body{font-family:Arial,sans-serif;max-width:720px;margin:32px auto;padding:0 16px;color:#1f2937}h1{color:#111827;margin-bottom:.25rem}dl{display:grid;grid-template-columns:160px 1fr;gap:.5rem 1rem}dt{font-weight:600}.note{color:#6b7280;font-size:.85rem;margin-top:1rem}table{border-collapse:collapse;margin-top:1rem;width:100%}th,td{border:1px solid #e5e7eb;padding:6px 10px;text-align:left}</style>")
          .Append("</head><body>")
          .Append("<h1>").Append(WebUtility.HtmlEncode(heading)).Append("</h1>")
          .Append("<dl>")
          .Append("<dt>").Append(WebUtility.HtmlEncode(keyValue.Key)).Append("</dt>")
          .Append("<dd>").Append(WebUtility.HtmlEncode(keyValue.Value)).Append("</dd>");

        if (secondary is { } sec)
        {
            sb.Append("<dt>").Append(WebUtility.HtmlEncode(sec.Key)).Append("</dt>")
              .Append("<dd>").Append(WebUtility.HtmlEncode(sec.Value)).Append("</dd>");
        }

        sb.Append("<dt>Generated</dt><dd>")
          .Append(DateTime.UtcNow.ToString("u"))
          .Append("</dd>")
          .Append("</dl>");

        if (!string.IsNullOrEmpty(extraBodyHtml))
        {
            sb.Append(extraBodyHtml);
        }

        sb.Append("<p class=\"note\">")
          .Append(WebUtility.HtmlEncode(note))
          .Append("</p></body></html>");

        return sb.ToString();
    }
}
