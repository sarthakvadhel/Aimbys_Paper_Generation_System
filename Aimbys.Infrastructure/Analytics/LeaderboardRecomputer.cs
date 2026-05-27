using Aimbys.Application.Analytics;
using Aimbys.Application.Scheduling;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Analytics;

/// <summary>
/// Nightly job that recomputes leaderboard rankings for all exams
/// that have results. Dispatched by the scheduling host via
/// <see cref="IScheduledJobHandler"/>.
/// </summary>
public sealed class LeaderboardRecomputer : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "analytics.leaderboard";

    /// <summary>Default cron: 03:30 UTC daily.</summary>
    public const string DefaultCron = "30 3 * * *";

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly ILogger<LeaderboardRecomputer> _logger;

    public LeaderboardRecomputer(
        AppDbContext db,
        IAnalyticsAggregationService aggregation,
        ILogger<LeaderboardRecomputer> logger)
    {
        _db = db;
        _aggregation = aggregation;
        _logger = logger;
    }

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        // V1 stub: no Exam entity exists yet on this branch. When the Exam
        // table lands (Chunks 25-29), this job will query all exams with
        // published results and recompute leaderboard entries. For now, log
        // and write a placeholder snapshot via the aggregation service using
        // a sentinel exam id.
        _logger.LogInformation(
            "LeaderboardRecomputer: no Exam data available yet (lands when Chunks 25-29 merge to main). Writing placeholder snapshot.");

        var sentinelExamId = Guid.Empty;
        await _aggregation.RecomputeLeaderboardAsync(sentinelExamId, cancellationToken);
    }
}
