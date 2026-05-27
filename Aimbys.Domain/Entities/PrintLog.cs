using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Audit trail for every PDF/document generation or download. Tracks
/// who printed what, when, and from which IP.
/// </summary>
public class PrintLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PaperVersionId { get; set; }
    public Guid? ResultId { get; set; }
    public PrintDocumentType DocumentType { get; set; }
    public string PrintedByUserId { get; set; } = string.Empty;
    public DateTime PrintedAtUtc { get; set; } = DateTime.UtcNow;
    public int CopyCount { get; set; } = 1;
    public string? IpAddress { get; set; }
    public Guid? InstituteId { get; set; }
}
