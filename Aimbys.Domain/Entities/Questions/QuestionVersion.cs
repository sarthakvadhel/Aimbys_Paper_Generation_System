using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Questions;

/// <summary>
/// Immutable content snapshot for a <see cref="Question"/>. Each edit
/// creates a new version; the approval workflow validates against the
/// latest version's fields.
/// </summary>
public class QuestionVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public int VersionNumber { get; set; } = 1;

    /// <summary>Rich-text body of the question (HTML).</summary>
    public string BodyHtml { get; set; } = string.Empty;

    public DifficultyLevel Difficulty { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public decimal Marks { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public string? InstructionsHtml { get; set; }
    public bool IsCurrentVersion { get; set; } = true;
    public string AuthorUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // ----- File-upload support (Chunk 34) ------------------------------------

    /// <summary>Comma-separated MIME allow-list for file upload questions.</summary>
    public string? AllowedMimeTypes { get; set; }

    /// <summary>Max file size for file upload (default concept: 10 MB).</summary>
    public long? MaxFileSizeBytes { get; set; }

    // ----- Navigation -------------------------------------------------------

    public Question? Question { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuestionRubricCriterion> RubricCriteria { get; set; } = new List<QuestionRubricCriterion>();
    public ICollection<QuestionTestCase> TestCases { get; set; } = new List<QuestionTestCase>();
}
