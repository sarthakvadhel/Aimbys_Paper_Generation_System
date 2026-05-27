namespace Aimbys.Domain.Enums;

/// <summary>
/// Lifecycle of an <see cref="Aimbys.Domain.Entities.Institute"/> as it
/// passes through Super Admin governance:
///
/// <list type="bullet">
///   <item><see cref="PendingApproval"/> &mdash; tenant has been provisioned and is awaiting Super Admin approval.</item>
///   <item><see cref="Active"/> &mdash; in service.</item>
///   <item><see cref="Suspended"/> &mdash; temporarily disabled; data preserved.</item>
///   <item><see cref="Rejected"/> &mdash; never approved; effectively a soft-deleted onboarding attempt.</item>
///   <item><see cref="Archived"/> &mdash; decommissioned tenant; read-only reference data.</item>
/// </list>
/// </summary>
public enum InstituteStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
    Rejected = 3,
    Archived = 4
}
