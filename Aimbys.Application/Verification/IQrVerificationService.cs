namespace Aimbys.Application.Verification;

public interface IQrVerificationService
{
    /// <summary>Generates a verification token (paperId:sha256Hash encoded as base64url).</summary>
    string GenerateToken(Guid paperId, string contentHash);

    /// <summary>Parses and validates a token. Returns the result.</summary>
    Task<QrVerificationResult> VerifyAsync(string token, CancellationToken ct = default);
}

public sealed record QrVerificationResult(
    bool IsValid,
    Guid? PaperId,
    string? PaperTitle,
    string? InstituteName,
    DateTime? GeneratedAtUtc,
    string? Reason);
