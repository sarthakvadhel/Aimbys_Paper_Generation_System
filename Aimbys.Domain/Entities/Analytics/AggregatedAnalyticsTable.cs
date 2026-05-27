using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Analytics;

public class AggregatedAnalyticsTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnalyticsScope Scope { get; set; }
    public Guid ScopeId { get; set; }
    public string DimensionKey { get; set; } = string.Empty;
    public string DimensionValue { get; set; } = string.Empty;
    public string MetricJson { get; set; } = "{}";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
