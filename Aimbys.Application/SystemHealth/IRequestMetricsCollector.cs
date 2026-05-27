namespace Aimbys.Application.SystemHealth;

/// <summary>
/// In-memory rolling-window collector for HTTP request metrics.
/// Implementations track the last 24h of per-minute buckets and are
/// queried by the SuperAdmin SystemHealth screen. Ephemeral by
/// design &mdash; not persisted across restarts.
/// </summary>
public interface IRequestMetricsCollector
{
    /// <summary>Records a single completed request.</summary>
    void Record(int statusCode, long elapsedMs);

    /// <summary>Returns a snapshot of the last 24h of metrics.</summary>
    SystemHealthSnapshot GetSnapshot();
}

/// <summary>
/// Aggregate snapshot for the last 24h of request metrics. Carries
/// the per-minute buckets plus pre-computed totals so the view layer
/// can render KPI cards without re-summing.
/// </summary>
public sealed record SystemHealthSnapshot(
    IReadOnlyList<MetricsBucket> Buckets,
    int TotalRequests,
    int ErrorRequests,
    double AverageResponseTimeMs);

/// <summary>
/// One per-minute bucket of request metrics. <see cref="BucketUtc"/>
/// is truncated to the minute (seconds and below set to zero).
/// </summary>
public sealed record MetricsBucket(
    DateTime BucketUtc,
    int RequestCount,
    int ErrorCount,
    double AverageResponseTimeMs);
