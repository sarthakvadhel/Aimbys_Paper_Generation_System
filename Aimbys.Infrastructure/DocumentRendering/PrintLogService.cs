using System.Security.Claims;
using Aimbys.Application.DocumentRendering;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;

namespace Aimbys.Infrastructure.DocumentRendering;

public sealed class PrintLogService : IPrintLogService
{
    private readonly AppDbContext _db;

    public PrintLogService(AppDbContext db) => _db = db;

    public async Task LogPrintAsync(
        PrintDocumentType documentType,
        Guid? paperVersionId,
        Guid? resultId,
        Guid? instituteId,
        ClaimsPrincipal actor,
        string? ipAddress,
        int copyCount = 1,
        CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var log = new PrintLog
        {
            DocumentType = documentType,
            PaperVersionId = paperVersionId,
            ResultId = resultId,
            InstituteId = instituteId,
            PrintedByUserId = userId,
            PrintedAtUtc = DateTime.UtcNow,
            CopyCount = copyCount,
            IpAddress = ipAddress
        };
        _db.Set<PrintLog>().Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
