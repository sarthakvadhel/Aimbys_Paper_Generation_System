using Aimbys.Domain.Enums;

namespace Aimbys.Web.Areas.SuperAdmin.ViewModels;

public sealed class SubscriptionListViewModel
{
    public IReadOnlyList<SubscriptionRow> Rows { get; set; } = Array.Empty<SubscriptionRow>();
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class SubscriptionRow
{
    public Guid InstituteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; }
    public InstituteSubscriptionStatus Status { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class SubscriptionEditViewModel
{
    public Guid InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; }
    public InstituteSubscriptionStatus Status { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int GracePeriodDays { get; set; } = 7;
}
