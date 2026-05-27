namespace Aimbys.Application.DocumentRendering;

/// <summary>
/// Boundary between the document-rendering pipeline (which produces
/// HTML) and whichever PDF engine the deployment ships with. Keeps
/// the engine choice (Puppeteer, wkhtmltopdf, QuestPDF, …) behind a
/// single seam so swapping it never changes <c>IDocumentRenderService</c>.
///
/// <para>
/// V1 ships a logging stub implementation; the contract returns the
/// HTML bytes wrapped in a "PDF-like" stream so callers can exercise
/// the full pipeline without the engine dependency.
/// </para>
/// </summary>
public interface IHtmlToPdfConverter
{
    /// <summary>
    /// Converts <paramref name="html"/> to PDF bytes. The returned
    /// stream is positioned at zero and owned by the caller.
    /// </summary>
    Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default);
}
