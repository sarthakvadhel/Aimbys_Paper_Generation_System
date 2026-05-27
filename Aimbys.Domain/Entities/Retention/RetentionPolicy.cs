namespace Aimbys.Domain.Entities.Retention;

/// <summary>
/// Per-entity retention rule. Read by
/// <c>RetentionEnforcementJobHandler</c> on its weekly sweep:
///
/// <list type="bullet">
///   <item>If <see cref="LegalHold"/> is <c>true</c>, the policy is
///         honoured but not enforced &mdash; the entity type is
///         frozen until the hold is released. The handler still
///         records an audit row noting the skip.</item>
///   <item>Otherwise, soft-deleted rows whose
///         <c>DeletedAtUtc</c> is older than
///         <see cref="RetentionDays"/> become candidates for the
///         configured <see cref="Aimbys.Domain.Enums.ArchiveStrategy"/>.</item>
/// </list>
///
/// <see cref="ArchiveAfterDays"/> is a softer threshold for "move to
/// archive table" semantics; a future chunk that introduces dedicated
/// archive tables consumes it. The current handler treats it as
/// informational.
/// </summary>
public class RetentionPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Discriminator naming the entity type the policy applies to,
    /// e.g. <c>"Institute"</c>, <c>"FileAsset"</c>. Globally unique;
    /// the handler uses it to dispatch to the right DbSet.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Days a soft-deleted row must remain in place before it becomes
    /// eligible for the configured archive strategy. Must be &gt;= 0;
    /// 0 means "process on the next sweep".
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Optional hint for "move to archive table" semantics. Honoured
    /// in a future chunk that introduces dedicated archive tables.
    /// </summary>
    public int ArchiveAfterDays { get; set; }

    /// <summary>
    /// Legal hold flag. While <c>true</c> the handler skips
    /// enforcement and writes a single "skipped due to legal hold"
    /// audit row per sweep so compliance has a paper trail.
    /// </summary>
    public bool LegalHold { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
