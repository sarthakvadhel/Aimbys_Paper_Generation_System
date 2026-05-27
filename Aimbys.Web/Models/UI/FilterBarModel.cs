namespace Aimbys.Web.Models.UI;

/// <summary>
/// View model for <c>_FilterBar.cshtml</c>. The bar emits a GET form
/// against the current page so chunked controllers can bind to its
/// values via standard query-string model binding.
/// </summary>
/// <param name="Action">Form action target. Defaults to the current page when null.</param>
/// <param name="SearchName">Name attribute for the search input. Default <c>q</c>.</param>
/// <param name="SearchValue">Pre-filled search query (for round-tripping).</param>
/// <param name="SearchPlaceholder">Placeholder text. Default "Search…".</param>
/// <param name="Filters">Optional select dropdowns rendered after the search input.</param>
public sealed record FilterBarModel(
    string? Action = null,
    string SearchName = "q",
    string? SearchValue = null,
    string SearchPlaceholder = "Search…",
    IReadOnlyList<FilterBarSelect>? Filters = null);

/// <summary>
/// One select dropdown inside the filter bar.
/// </summary>
/// <param name="Label">Visually-hidden label for screen readers; also used as the placeholder option.</param>
/// <param name="Name">Form field name.</param>
/// <param name="SelectedValue">Pre-selected option value, or null for "all".</param>
/// <param name="Options">List of (value, label) pairs.</param>
public sealed record FilterBarSelect(
    string Label,
    string Name,
    string? SelectedValue,
    IReadOnlyList<FilterBarOption> Options);

public sealed record FilterBarOption(string Value, string Label);
