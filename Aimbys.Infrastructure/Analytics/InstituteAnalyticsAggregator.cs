using Aimbys.Application.Analytics;
using Aimbys.Application.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Analytics;

/// <summary>
/// Nightly job that aggregates institute-level metrics for all active
/// institutes. Dispatched by the scheduling host via
/// <see cref="IScheduledJobHandler"/>.
/// </summary>
public sealed class InstituteAnalyticsAggregator : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "analytics.institute";

    /// <summary>Default cron: 02:00 UTC daily.</summary>
    public const string DefaultCron = "0 2 * * *";

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly ILogger<InstituteAnalyticsAggregator> _logger;

    public InstituteAnalyticsAggregator(
        AppDbContext db,
        IAnalyticsAggregationService aggregation,
        ILogger<InstituteAnalyticsAggregator> logger)
    {
        _db = db;
        _aggregation = aggregation;
        _logger = logger;
    }

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        var instituteIds = await _db.Institutes
            .Where(i => i.Status == InstituteStatus.Active)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "InstituteAnalyticsAggregator: processing {Count} active institutes.",
            instituteIds.Count);

        foreach (var id in instituteIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _aggregation.AggregateInstituteMetricsAsync(id, cancellationToken);
        }
    }
}
