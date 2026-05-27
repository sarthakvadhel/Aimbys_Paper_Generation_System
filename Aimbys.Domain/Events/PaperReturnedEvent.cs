namespace Aimbys.Domain.Events;

public sealed record PaperReturnedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;

    /// <summary>Identity user id of the paper's author (the recipient of the return notification).</summary>
    public string AuthorUserId { get; init; } = string.Empty;

    /// <summary>Identity user id of the institute admin who returned the paper.</summary>
    public string ReturnedByUserId { get; init; } = string.Empty;

    /// <summary>Reviewer comment explaining why the paper was returned. Required by the service.</summary>
    public string? Comment { get; init; }
}
