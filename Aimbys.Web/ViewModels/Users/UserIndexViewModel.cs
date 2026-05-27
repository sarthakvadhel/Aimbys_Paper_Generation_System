using Aimbys.Web.Models.UI;

namespace Aimbys.Web.ViewModels.Users;

public class UserIndexViewModel
{
    public DataTableModel Table { get; set; } = null!;
    public FilterBarModel FilterBar { get; set; } = null!;
    public PaginationModel Pagination { get; set; } = null!;
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int TotalCount { get; set; }
}
