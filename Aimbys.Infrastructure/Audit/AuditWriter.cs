using Aimbys.Application.Audit;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Aimbys.Infrastructure.Audit;

/// <summary>
/// Default <see cref="IAuditWriter"/> backed by <see cref="AppDbContext"/>.
/// Stages an <see cref="AuditLog"/> row on the same context the caller is
/// using so the audit row commits with the business change.
///
/// IP address is captured from the ambient <see cref="HttpContext"/> when
/// available; background workers (which have no request scope) emit rows
/// with a null IP.
/// </summary>
public sealed class AuditWriter : IAuditWriter
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditWriter(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        string? actorUserId,
        string? detailsJson = null,
        AuditSeverity severity = AuditSeverity.Information,
        CancellationToken cancellationToken = default)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = detailsJson,
            IpAddress = ipAddress,
            Severity = severity,
            OccurredAtUtc = DateTime.UtcNow
        });

        // Caller's unit-of-work commits.
        return Task.CompletedTask;
    }
}
