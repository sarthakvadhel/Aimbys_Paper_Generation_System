using Aimbys.Application.Bulk;

namespace Aimbys.Web.Areas.Institute.ViewModels;

public class BulkResultViewModel
{
    public bool HasResult { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
}

public sealed class BulkActivationViewModel : BulkResultViewModel
{
    public bool Activate { get; set; } = true;
}
