using Aimbys.Domain.Enums;

namespace Aimbys.Application.Institutes;

/// <summary>
/// Input contract for creating a new institute and its initial admin user.
/// </summary>
public sealed record InstituteCreateRequest(
    string Name,
    string Code,
    InstituteType Type,
    string City,
    string State,
    string Country,
    string ContactEmail,
    string? ContactPhone,
    LicenseTier LicenseTier,
    string AdminEmail);
