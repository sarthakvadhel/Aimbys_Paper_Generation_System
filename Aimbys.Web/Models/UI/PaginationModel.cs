namespace Aimbys.Web.Models.UI;

/// <summary>
/// View model for <c>_PaginationPartial.cshtml</c>. The partial renders
/// a Bootstrap pagination component whose Previous / page-number /
/// Next links preserve the current request's query string and just
/// flip the page-number parameter.
/// </summary>
/// <param name="CurrentPage">1-based page index.</param>
/// <param name="TotalPages">Total page count. Values &lt;= 1 hide the pagination.</param>
/// <param name="PageQueryName">Query-string parameter name. Default <c>page</c>.</param>
public sealed record PaginationModel(
    int CurrentPage,
    int TotalPages,
    string PageQueryName = "page");
