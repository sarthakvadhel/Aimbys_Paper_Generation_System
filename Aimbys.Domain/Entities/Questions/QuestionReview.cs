using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Questions;

public class QuestionReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public Guid ReviewerTeacherProfileId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public ReviewVerdict Verdict { get; set; } = ReviewVerdict.Pending;
    public string? Comment { get; set; }
}
