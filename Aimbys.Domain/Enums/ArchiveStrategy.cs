namespace Aimbys.Domain.Enums;

/// <summary>
/// What the retention/archive job does once an entity row passes its
/// retention window. Defaults are conservative &mdash;
/// <see cref="SoftArchive"/> simply preserves the soft-delete and
/// stops touching the row, while
/// <see cref="Purge"/> permanently removes it.
/// </summary>
public enum ArchiveStrategy
{
    /// <summary>
    /// Leave the soft-deleted row in place; no further action. Useful
    /// when the retention window is already long enough and the audit
    /// row of "soft-deleted at X" is the sufficient permanent record.
    /// </summary>
    SoftArchive = 0,

    /// <summary>
    /// Serialise the row to a JSON document under the configured
    /// archive folder before purging. Lets the platform meet
    /// "we may delete but you can request a copy" obligations.
    /// </summary>
    Export = 1,

    /// <summary>
    /// Permanent hard-delete. Only allowed when no legal-hold is in
    /// effect; the job writes a Warning-severity audit row.
    /// </summary>
    Purge = 2
}
