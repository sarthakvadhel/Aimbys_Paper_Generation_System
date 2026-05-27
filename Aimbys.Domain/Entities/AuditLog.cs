using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Append-only audit row written for every state-changing action. Uses a
/// long identity key because this table is high-volume.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ASP.NET Identity user id of the actor; null for anonymous / system events.
    /// </summary>
    public string? ActorUserId { get; set; }

    /// <summary>Action verb, e.g. "Project.Created", "Document.Published".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Domain entity type the action targeted, e.g. "Project".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>String form of the entity primary key.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Free-form serialized detail payload.</summary>
    public string? DetailsJson { get; set; }

    /// <summary>IP address of the requester (IPv4 or IPv6).</summary>
    public string? IpAddress { get; set; }

    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
}
