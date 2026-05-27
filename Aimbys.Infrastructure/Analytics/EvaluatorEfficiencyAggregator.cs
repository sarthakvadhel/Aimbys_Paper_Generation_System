using Aimbys.Application.Analytics;
using Aimbys.Application.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Analytics;

/// <summary>
/// Nightly job that aggregates evaluator efficiency metrics for all
/// active institutes. Dispatched by the scheduling host via
/// <see cref="IScheduledJobHandler"/>.
/// </summary>
public sealed class EvaluatorEfficiencyAggregator : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "analytics.evaluator-efficiency";

    /// <summary>Default cron: 03:00 UTC daily.</summary>
    public const string DefaultCron = "0 3 * * *";

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly ILogger<EvaluatorEfficiencyAggregator> _logger;

    public EvaluatorEfficiencyAggregator(
        AppDbContext db,
        IAnalyticsAggregationService aggregation,
        ILogger<EvaluatorEfficiencyAggregator> logger)
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
            "EvaluatorEfficiencyAggregator: processing {Count} active institutes.",
            instituteIds.Count);

        foreach (var id in instituteIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _aggregation.AggregateEvaluatorEfficiencyAsync(id, cancellationToken);
        }
    }
}
