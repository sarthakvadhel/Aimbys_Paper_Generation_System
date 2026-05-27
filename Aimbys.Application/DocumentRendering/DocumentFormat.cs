namespace Aimbys.Application.DocumentRendering;

/// <summary>
/// Output format requested from <c>IDocumentRenderService</c>.
/// V1 ships only the HTML pipeline; PDF flows through
/// <c>IHtmlToPdfConverter</c> which currently has a logging stub
/// implementation. A production converter (Puppeteer / wkhtmltopdf /
/// QuestPDF) plugs in by implementing <c>IHtmlToPdfConverter</c>.
/// </summary>
public enum DocumentFormat
{
    Html = 0,
    Pdf = 1
}
