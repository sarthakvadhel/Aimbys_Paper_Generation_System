using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Analytics;

public class AnalyticsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnalyticsScope Scope { get; set; }
    public Guid ScopeId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public string MetricValueJson { get; set; } = "null";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}
