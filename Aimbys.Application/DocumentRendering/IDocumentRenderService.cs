namespace Aimbys.Application.DocumentRendering;

/// <summary>
/// Single sanctioned route for generating exportable documents
/// (papers, result sheets, certificates, transcripts, analytics
/// exports). Hides the HTML-template / PDF-converter pipeline from
/// callers so a future change of engine is a non-event for
/// controllers.
///
/// <para>
/// V1 surfaces produce HTML; the PDF path goes through
/// <see cref="IHtmlToPdfConverter"/> which today has a logging stub.
/// </para>
/// </summary>
public interface IDocumentRenderService
{
    /// <summary>
    /// Renders the paper identified by <paramref name="paperId"/> in
    /// the requested <paramref name="format"/>.
    /// </summary>
    Task<RenderedDocument> RenderPaperAsync(
        Guid paperId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the result sheet for one exam attempt.
    /// </summary>
    Task<RenderedDocument> RenderResultSheetAsync(
        Guid attemptId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a per-student per-exam certificate.
    /// </summary>
    Task<RenderedDocument> RenderCertificateAsync(
        Guid studentId,
        Guid examId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders the cumulative transcript for a single student.
    /// </summary>
    Task<RenderedDocument> RenderTranscriptAsync(
        Guid studentId,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders an ad-hoc analytics export. <paramref name="scope"/> is
    /// a coarse selector (e.g. <c>"institute-overview"</c>,
    /// <c>"teacher-performance"</c>); <paramref name="filters"/>
    /// supplies the bound parameters.
    /// </summary>
    Task<RenderedDocument> RenderAnalyticsExportAsync(
        string scope,
        IReadOnlyDictionary<string, string> filters,
        DocumentFormat format = DocumentFormat.Html,
        CancellationToken cancellationToken = default);
}
