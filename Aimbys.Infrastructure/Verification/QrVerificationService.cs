using System.Text;
using Aimbys.Application.Verification;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Verification;

/// <summary>
/// Implements QR-based verification of printed papers. Tokens are
/// base64url-encoded strings of the form "paperId:contentHash".
/// Verification decodes the token, looks up the paper in the DB,
/// and returns validity status.
/// </summary>
public sealed class QrVerificationService : IQrVerificationService
{
    private readonly AppDbContext _db;

    public QrVerificationService(AppDbContext db) => _db = db;

    public string GenerateToken(Guid paperId, string contentHash)
    {
        var raw = $"{paperId}:{contentHash}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Base64UrlEncode(bytes);
    }

    public async Task<QrVerificationResult> VerifyAsync(string token, CancellationToken ct = default)
    {
        byte[] decoded;
        try
        {
            decoded = Base64UrlDecode(token);
        }
        catch
        {
            return new QrVerificationResult(false, null, null, null, null, "Invalid token format.");
        }

        var raw = Encoding.UTF8.GetString(decoded);
        var separatorIndex = raw.IndexOf(':');
        if (separatorIndex < 0)
            return new QrVerificationResult(false, null, null, null, null, "Malformed token payload.");

        var paperIdStr = raw[..separatorIndex];
        if (!Guid.TryParse(paperIdStr, out var paperId))
            return new QrVerificationResult(false, null, null, null, null, "Invalid paper identifier.");

        var paper = await _db.Papers
            .AsNoTracking()
            .Where(p => p.Id == paperId)
            .Select(p => new
            {
                p.Id,
                p.InstituteId,
                p.CreatedAtUtc,
                Title = _db.PaperVersions
                    .Where(v => v.PaperId == p.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => v.Title)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (paper is null)
            return new QrVerificationResult(false, paperId, null, null, null, "Paper not found.");

        var instituteName = await _db.Institutes
            .AsNoTracking()
            .Where(i => i.Id == paper.InstituteId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct);

        return new QrVerificationResult(
            IsValid: true,
            PaperId: paper.Id,
            PaperTitle: paper.Title ?? "Untitled",
            InstituteName: instituteName ?? "Unknown",
            GeneratedAtUtc: paper.CreatedAtUtc,
            Reason: null);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
