namespace Aimbys.Web.Models.UI;

/// <summary>
/// View model for <c>_DataTable.cshtml</c>. Keeps the partial fully
/// generic: callers project their domain rows into
/// <see cref="DataTableRow"/> instances and the partial renders them
/// without ever seeing the underlying entity type.
/// </summary>
/// <param name="Caption">Visually-hidden table caption for screen readers.</param>
/// <param name="Columns">Column headers; the count must match the cells in every row.</param>
/// <param name="Rows">Data rows.</param>
/// <param name="EmptyMessage">Shown when <see cref="Rows"/> is empty.</param>
public sealed record DataTableModel(
    string Caption,
    IReadOnlyList<DataTableColumn> Columns,
    IReadOnlyList<DataTableRow> Rows,
    string EmptyMessage = "No rows to display.");

/// <summary>
/// One header column. <see cref="Align"/> drives Bootstrap text-end
/// when the column is numeric.
/// </summary>
public sealed record DataTableColumn(
    string Title,
    DataTableAlign Align = DataTableAlign.Start);

public enum DataTableAlign
{
    Start = 0,
    Center = 1,
    End = 2
}

/// <summary>
/// One row of cells. The partial preserves cell order so the count
/// must match <see cref="DataTableModel.Columns"/>.
/// </summary>
public sealed record DataTableRow(IReadOnlyList<DataTableCell> Cells);

/// <summary>
/// One cell. <see cref="IsHtml"/> = <c>true</c> opts in to raw HTML
/// rendering (used for status badges); the default treats
/// <see cref="Text"/> as plain text and HTML-encodes it.
/// </summary>
public sealed record DataTableCell(string Text, bool IsHtml = false);
