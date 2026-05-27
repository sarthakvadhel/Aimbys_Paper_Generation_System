namespace Aimbys.Domain.Enums;

/// <summary>
/// Logical file-storage areas. Each area maps 1:1 to a fixed sub-directory of
/// the storage root and to a permission policy in
/// <see cref="Aimbys.Domain.Permissions.FileAreaPolicies"/>.
///
/// The folder map is fixed by design &mdash; callers cannot inject arbitrary
/// directory names. Future areas require a code change here, not a config
/// change.
/// </summary>
public enum FileArea
{
    /// <summary>Question-bank assets (TinyMCE images, attachments).</summary>
    Questions    = 0,
    /// <summary>Generated paper artefacts (PDF, DOCX print masters).</summary>
    Papers       = 1,
    /// <summary>Student answer uploads (descriptive answer attachments, file-upload questions).</summary>
    Answers      = 2,
    /// <summary>Issued certificates and result PDFs.</summary>
    Certificates = 3,
    /// <summary>Exported analytics / audit reports.</summary>
    Reports      = 4,
    /// <summary>Per-exam artefacts (seating plans, instruction sheets).</summary>
    Exams        = 5,
    /// <summary>Coding-exam submissions (source bundles, execution logs).</summary>
    Coding       = 6,
    /// <summary>Short-lived scratch space; subject to TTL cleanup in a later chunk.</summary>
    Temp         = 7
}
