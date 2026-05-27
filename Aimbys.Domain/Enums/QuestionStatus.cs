namespace Aimbys.Domain.Enums;

/// <summary>
/// Lifecycle states for a <c>Question</c>. Mirrors the states declared
/// in the <c>QuestionApproval</c> workflow definition.
/// </summary>
public enum QuestionStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Retired = 5,
    Archived = 6
}
