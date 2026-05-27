using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Questions;

/// <summary>
/// Root aggregate for a question in the question bank. Tracks its
/// lifecycle status and links to versioned content via <see cref="QuestionVersion"/>.
/// </summary>
public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid AuthorTeacherProfileId { get; set; }
    public Guid? CurrentVersionId { get; set; }

    /// <summary>Identity user id of the author who created this question.</summary>
    public string AuthorUserId { get; set; } = string.Empty;

    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;
    public QuestionType Type { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ----- Case-study support (Chunk 34) ------------------------------------

    /// <summary>Self-referencing FK for case-study sub-questions.</summary>
    public Guid? ParentQuestionId { get; set; }

    /// <summary>Rich-text context passage for the case-study parent question.</summary>
    public string? CaseStudyContextHtml { get; set; }

    /// <summary>Navigation to the parent case-study question.</summary>
    public Question? ParentQuestion { get; set; }

    /// <summary>Sub-questions belonging to this case-study parent.</summary>
    public ICollection<Question> SubQuestions { get; set; } = new List<Question>();

    // ----- Navigation -------------------------------------------------------

    public ICollection<QuestionVersion> Versions { get; set; } = new List<QuestionVersion>();
}
