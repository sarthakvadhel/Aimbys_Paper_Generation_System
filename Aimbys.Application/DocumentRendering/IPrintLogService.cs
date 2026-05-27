using System.Security.Claims;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.DocumentRendering;

public interface IPrintLogService
{
    Task LogPrintAsync(
        PrintDocumentType documentType,
        Guid? paperVersionId,
        Guid? resultId,
        Guid? instituteId,
        ClaimsPrincipal actor,
        string? ipAddress,
        int copyCount = 1,
        CancellationToken ct = default);
}
