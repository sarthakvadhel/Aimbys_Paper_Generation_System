namespace Aimbys.Application.Bulk;

/// <summary>
/// One error captured during a bulk operation. Per-row reporting is
/// the contract for every <c>IBulkOperationService</c> method &mdash;
/// callers never get a "success or one error" boolean; they get a
/// structured list they can render row-by-row in the import UI.
/// </summary>
/// <param name="RowNumber">
/// 1-based row index inside the input batch. Use 0 for batch-level
/// failures that don't map to a specific row.
/// </param>
/// <param name="ErrorCode">
/// Stable machine-readable code (e.g. <c>"row.email_invalid"</c>,
/// <c>"row.class_batch_not_found"</c>). Controllers / tests
/// pattern-match on this; the message is for humans.
/// </param>
/// <param name="Message">Human-readable explanation.</param>
/// <param name="Field">Optional source-column name, useful for highlighting in the UI.</param>
public sealed record BulkOperationError(
    int RowNumber,
    string ErrorCode,
    string Message,
    string? Field = null);
