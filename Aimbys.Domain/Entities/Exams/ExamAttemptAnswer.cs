using Aimbys.Domain.Entities;

namespace Aimbys.Domain.Entities.Exams;

public class ExamAttemptAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public string? AnswerJson { get; set; }
    public bool IsFlagged { get; set; }
    public decimal? AutoMarksAwarded { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }

    /// <summary>FK to FileAsset for file-upload answers (Chunk 34).</summary>
    public Guid? FileAssetId { get; set; }

    // Navigation
    public ExamAttempt? Attempt { get; set; }
    public FileAsset? FileAsset { get; set; }
}
