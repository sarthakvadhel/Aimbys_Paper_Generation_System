namespace Aimbys.Domain.Enums;

/// <summary>
/// Visual severity tier for in-app notifications. Maps to Bootstrap alert
/// classes in the activity-feed view.
/// </summary>
public enum NotificationSeverity
{
    Information = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
