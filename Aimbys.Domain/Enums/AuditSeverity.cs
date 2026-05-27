namespace Aimbys.Domain.Enums;

/// <summary>
/// Severity levels for <see cref="Aimbys.Domain.Entities.AuditLog"/> entries.
/// Ordered so default(AuditSeverity) == Information.
/// </summary>
public enum AuditSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}
