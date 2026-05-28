namespace Aimbys.Domain.Events;

public sealed record ResultPublishedEvent : DomainEventBase
{
    public Guid ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public string PublishedByUserId { get; init; } = string.Empty;
    public int StudentCount { get; init; }

    /// <summary>
    /// Identity user ids of every student whose result was published in
    /// this batch. The notification projection fans out one Notification
    /// per recipient. May contain a single id (per-attempt auto-publish
    /// path) or many ids (institute-admin batch publish).
    /// </summary>
    public IReadOnlyList<string> RecipientUserIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Per-recipient deep-link target. Either:
    /// <list type="bullet">
    ///   <item>An exact attempt-id route &mdash; only used when
    ///         <see cref="RecipientUserIds"/> contains exactly one
    ///         student.</item>
    ///   <item><c>null</c> &mdash; the projection uses the generic
    ///         <c>/Student/Results</c> index URL.</item>
    /// </list>
    /// </summary>
    public Guid? AttemptId { get; init; }
}
