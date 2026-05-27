using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Scheduling;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Retention;
using Aimbys.Domain.Enums;
using Aimbys.Domain.SoftDelete;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Retention;

/// <summary>
/// Scheduled-job handler that walks every
/// <see cref="RetentionPolicy"/> row and, for each non-held policy,
/// applies the configured
/// <see cref="Aimbys.Domain.Entities.Retention.ArchivePolicy"/> to
/// soft-deleted entities past their retention window.
///
/// <para>
/// Invoked by <c>ISchedulingService</c> &mdash; the platform
/// registers a recurring "<c>retention.enforce</c>" job at startup
/// (default cadence: weekly).
/// </para>
///
/// <para>
/// V1 dispatches against the two entity types that implement
/// <see cref="ISoftDelete"/> today (<c>Institute</c>,
/// <c>FileAsset</c>). New entity types are picked up by extending the
/// <see cref="ApplyForEntityTypeAsync"/> switch &mdash; explicit
/// dispatch is preferred over reflection so the set of governed
/// types is greppable.
/// </para>
/// </summary>
public sealed class RetentionEnforcementJobHandler : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "retention.enforce";

    /// <summary>
    /// Default cron expression used when the job is registered at
    /// startup. Public so tests / startup code can reference the same
    /// constant.
    /// </summary>
    public const string DefaultCron = "0 3 * * 0"; // 03:00 UTC every Sunday

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly ILogger<RetentionEnforcementJobHandler> _logger;

    public RetentionEnforcementJobHandler(
        AppDbContext db,
        IAuditWriter audit,
        ILogger<RetentionEnforcementJobHandler> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        var policies = await _db.Set<RetentionPolicy>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (policies.Count == 0)
        {
            _logger.LogInformation("Retention enforcement skipped: no RetentionPolicy rows configured.");
            return;
        }

        var archives = await _db.Set<ArchivePolicy>()
            .AsNoTracking()
            .ToDictionaryAsync(a => a.EntityType, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var policy in policies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Legal-hold gate. Per the spec we still write an audit
            // row each sweep so compliance has a "yes the policy was
            // checked, no action taken" record.
            if (policy.LegalHold)
            {
                await _audit.WriteAsync(
                    "Retention.SkippedDueToLegalHold",
                    entityType: policy.EntityType,
                    entityId: policy.Id.ToString(),
                    actorUserId: null,
                    detailsJson: JsonSerializer.Serialize(new { policy.RetentionDays }),
                    severity: AuditSeverity.Information,
                    cancellationToken: cancellationToken);
                continue;
            }

            var strategy = archives.TryGetValue(policy.EntityType, out var ap)
                ? ap.Strategy
                : ArchiveStrategy.SoftArchive; // safest default

            var cutoff = DateTime.UtcNow.AddDays(-policy.RetentionDays);
            var processed = await ApplyForEntityTypeAsync(
                policy.EntityType, cutoff, strategy, cancellationToken);

            await _audit.WriteAsync(
                "Retention.Enforced",
                entityType: policy.EntityType,
                entityId: policy.Id.ToString(),
                actorUserId: null,
                detailsJson: JsonSerializer.Serialize(new
                {
                    strategy = strategy.ToString(),
                    cutoffUtc = cutoff,
                    processed
                }),
                severity: strategy == ArchiveStrategy.Purge ? AuditSeverity.Warning : AuditSeverity.Information,
                cancellationToken: cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Dispatches by entity-type discriminator. Returns the count of
    /// rows acted on. Adding a new soft-deletable entity to the
    /// retention regime is a one-line change here plus a
    /// <c>RetentionPolicy</c> row in seed data.
    /// </summary>
    private Task<int> ApplyForEntityTypeAsync(
        string entityType,
        DateTime cutoff,
        ArchiveStrategy strategy,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            nameof(Institute) =>
                ApplyAsync(_db.Institutes, cutoff, strategy, cancellationToken),
            nameof(FileAsset) =>
                ApplyAsync(_db.FileAssets, cutoff, strategy, cancellationToken),
            _ => UnknownEntityTypeAsync(entityType)
        };
    }

    private Task<int> UnknownEntityTypeAsync(string entityType)
    {
        _logger.LogWarning(
            "RetentionPolicy references unknown EntityType '{EntityType}'. Add a switch arm in RetentionEnforcementJobHandler.",
            entityType);
        return Task.FromResult(0);
    }

    /// <summary>
    /// Applies <paramref name="strategy"/> to soft-deleted rows of
    /// <typeparamref name="T"/> whose <c>DeletedAtUtc</c> is older
    /// than <paramref name="cutoff"/>. The query uses
    /// <c>IgnoreQueryFilters</c> so it actually sees the soft-deleted
    /// rows the global filter would otherwise hide.
    /// </summary>
    private async Task<int> ApplyAsync<T>(
        DbSet<T> set,
        DateTime cutoff,
        ArchiveStrategy strategy,
        CancellationToken cancellationToken)
        where T : class, ISoftDelete
    {
        var candidates = await set
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted && e.DeletedAtUtc != null && e.DeletedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        switch (strategy)
        {
            case ArchiveStrategy.SoftArchive:
                // No-op: rows stay soft-deleted. Audit row in caller.
                break;

            case ArchiveStrategy.Export:
                // V1: exporting payloads outside the database (to a
                // file share / object store) is the storage chunk's
                // responsibility. We log and treat as SoftArchive so
                // the row isn't lost while the export adapter lands.
                _logger.LogInformation(
                    "Export strategy is not yet wired to persistent archive storage; "
                  + "treating {Count} rows of {EntityType} as SoftArchive for now.",
                    candidates.Count, typeof(T).Name);
                break;

            case ArchiveStrategy.Purge:
                set.RemoveRange(candidates);
                break;
        }

        return candidates.Count;
    }
}
