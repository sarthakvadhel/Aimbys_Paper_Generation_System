using Aimbys.Application.Bulk;

namespace Aimbys.Web.ViewModels.Users;

public class BulkImportResultViewModel
{
    public int SuccessCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    public bool HasResult { get; set; }
    public IReadOnlyList<BulkCredential> Credentials { get; set; } = Array.Empty<BulkCredential>();
}
