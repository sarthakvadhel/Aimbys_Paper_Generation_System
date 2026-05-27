namespace Aimbys.Domain.Events;

public sealed record PaperApprovedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;
    public string ApprovedByUserId { get; init; } = string.Empty;

    /// <summary>
    /// Identity user id of the paper's author. Populated by the service
    /// so notification projections can address the teacher who wrote
    /// the paper instead of (incorrectly) the approving admin.
    /// </summary>
    public string AuthorUserId { get; init; } = string.Empty;
}
