using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Questions;

public class QuestionModeration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid ModeratorTeacherProfileId { get; set; }
    public Guid OriginalReviewId { get; set; }
    public ReviewVerdict FinalVerdict { get; set; }
    public string? Comment { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
