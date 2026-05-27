namespace Aimbys.Web.Models.UI;

public sealed record HeatmapModel(IReadOnlyList<string> Columns, IReadOnlyList<HeatmapRow> Rows);

public sealed record HeatmapRow(string Label, IReadOnlyList<int> Values);
