using Aimbys.Web.Models.UI;

namespace Aimbys.Web.ViewModels.Institutes;

public class InstituteIndexViewModel
{
    public DataTableModel Table { get; set; } = null!;
    public FilterBarModel FilterBar { get; set; } = null!;
    public PaginationModel Pagination { get; set; } = null!;
}
