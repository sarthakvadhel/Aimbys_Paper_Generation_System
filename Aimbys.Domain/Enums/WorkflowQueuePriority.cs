namespace Aimbys.Domain.Enums;

/// <summary>
/// Priority tier for items sitting in an <c>ApprovalQueue</c> or
/// <c>ModerationQueue</c>. Higher priority items surface first in inbox
/// queries; the integer values match Bootstrap badge severities so views
/// can render them without translation.
/// </summary>
public enum WorkflowQueuePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
