using System.Text;
using Aimbys.Application.DocumentRendering;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.DocumentRendering;

/// <summary>
/// V1 stub implementation of <see cref="IHtmlToPdfConverter"/>. Logs
/// the conversion request, then returns the HTML bytes wrapped in a
/// stream &mdash; not a real PDF, but enough that the document
/// pipeline can be exercised end-to-end before a production engine
/// (Puppeteer, wkhtmltopdf, QuestPDF) is wired up.
///
/// <para>
/// Production deployments register a real implementation in DI which
/// transparently replaces this stub.
/// </para>
/// </summary>
public sealed class LoggingHtmlToPdfConverter : IHtmlToPdfConverter
{
    private readonly ILogger<LoggingHtmlToPdfConverter> _logger;

    public LoggingHtmlToPdfConverter(ILogger<LoggingHtmlToPdfConverter> logger)
    {
        _logger = logger;
    }

    public Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "LoggingHtmlToPdfConverter received {Bytes} bytes of HTML; returning HTML payload as a PDF placeholder. " +
            "Register a production IHtmlToPdfConverter to emit real PDF output.",
            Encoding.UTF8.GetByteCount(html));

        var bytes = Encoding.UTF8.GetBytes(html);
        Stream stream = new MemoryStream(bytes, writable: false);
        return Task.FromResult(stream);
    }
}
