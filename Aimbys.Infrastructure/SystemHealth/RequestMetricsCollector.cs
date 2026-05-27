using System.Collections.Concurrent;
using Aimbys.Application.SystemHealth;

namespace Aimbys.Infrastructure.SystemHealth;

/// <summary>
/// In-memory rolling-window <see cref="IRequestMetricsCollector"/>.
/// Buckets keyed by minute (seconds truncated). Each
/// <see cref="Record"/> call updates the current bucket atomically and
/// also drops buckets older than 24h so the dictionary never grows
/// unbounded across long-running deployments.
///
/// <para>
/// Registered as a singleton: one collector per process. Ephemeral by
/// design &mdash; restarts wipe history. Persisted observability is
/// the job of an APM, not this collector.
/// </para>
/// </summary>
public sealed class RequestMetricsCollector : IRequestMetricsCollector
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<DateTime, BucketState> _buckets = new();

    public void Record(int statusCode, long elapsedMs)
    {
        var key = TruncateToMinute(DateTime.UtcNow);
        var state = _buckets.GetOrAdd(key, _ => new BucketState());
        state.Record(statusCode, elapsedMs);

        // Cheap cleanup: every record drops anything outside the window.
        // Doing it inline avoids a background timer and keeps the
        // collector fully self-contained.
        var cutoff = DateTime.UtcNow - Window;
        foreach (var k in _buckets.Keys)
        {
            if (k < cutoff)
            {
                _buckets.TryRemove(k, out _);
            }
        }
    }

    public SystemHealthSnapshot GetSnapshot()
    {
        var nowMinute = TruncateToMinute(DateTime.UtcNow);
        var startMinute = nowMinute - Window;

        // Materialise the per-minute series, padding missing buckets
        // with zeros so the chart x-axis is contiguous.
        var buckets = new List<MetricsBucket>(24 * 60);
        int totalRequests = 0;
        int totalErrors = 0;
        long totalElapsed = 0;

        for (var t = startMinute + TimeSpan.FromMinutes(1); t <= nowMinute; t = t.AddMinutes(1))
        {
            if (_buckets.TryGetValue(t, out var state))
            {
                var (count, errors, elapsed) = state.Snapshot();
                var avg = count == 0 ? 0d : (double)elapsed / count;
                buckets.Add(new MetricsBucket(t, count, errors, avg));
                totalRequests += count;
                totalErrors += errors;
                totalElapsed += elapsed;
            }
            else
            {
                buckets.Add(new MetricsBucket(t, 0, 0, 0));
            }
        }

        var avgResponse = totalRequests == 0 ? 0d : (double)totalElapsed / totalRequests;
        return new SystemHealthSnapshot(buckets, totalRequests, totalErrors, avgResponse);
    }

    private static DateTime TruncateToMinute(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Per-minute bucket. Updates are guarded by <c>lock(this)</c>
    /// because the three counters must move together to keep the
    /// average correct.
    /// </summary>
    private sealed class BucketState
    {
        private int _count;
        private int _errors;
        private long _elapsedTotalMs;

        public void Record(int statusCode, long elapsedMs)
        {
            lock (this)
            {
                _count++;
                if (statusCode >= 500)
                {
                    _errors++;
                }
                _elapsedTotalMs += elapsedMs;
            }
        }

        public (int Count, int Errors, long ElapsedTotalMs) Snapshot()
        {
            lock (this)
            {
                return (_count, _errors, _elapsedTotalMs);
            }
        }
    }
}
