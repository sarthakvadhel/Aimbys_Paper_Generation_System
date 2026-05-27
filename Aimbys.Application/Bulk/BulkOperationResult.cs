namespace Aimbys.Application.Bulk;

/// <summary>
/// Outcome of a bulk operation. Total = Succeeded + Failed; the
/// caller can render a "X of Y rows imported, Z errors" summary plus
/// the per-row error list.
/// </summary>
public sealed class BulkOperationResult
{
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<BulkOperationError> Errors { get; init; } = Array.Empty<BulkOperationError>();
    public List<BulkCredential> Credentials { get; } = new();

    public int Total => Succeeded + Failed;

    public static BulkOperationResult Empty { get; } = new();

    public static BulkOperationResult Create(int succeeded, IReadOnlyList<BulkOperationError> errors) => new()
    {
        Succeeded = succeeded,
        Failed = errors.Count,
        Errors = errors
    };
}

/// <summary>Email + temporary password pair returned after bulk import.</summary>
public sealed record BulkCredential(string Email, string TemporaryPassword);
