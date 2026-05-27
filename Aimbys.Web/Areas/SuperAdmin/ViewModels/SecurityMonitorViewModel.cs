namespace Aimbys.Web.Areas.SuperAdmin.ViewModels;

public sealed class SecurityMonitorViewModel
{
    public int EventsLast24Hours { get; init; }
    public int HighSeverityEventsLast24Hours { get; init; }
    public int DistinctActorsLast24Hours { get; init; }
    public IReadOnlyList<SecurityEventRowViewModel> RecentEvents { get; init; } = Array.Empty<SecurityEventRowViewModel>();
}

public sealed class SecurityEventRowViewModel
{
    public DateTime OccurredAtUtc { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
    public string Severity { get; init; } = "Information";
}
