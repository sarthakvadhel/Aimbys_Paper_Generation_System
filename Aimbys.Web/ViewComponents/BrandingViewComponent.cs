using Aimbys.Application.Authorization;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Resolves the current institute's branding (logo URL + primary
/// colour) for the role layout sidebar header. Falls back to the
/// platform default when no institute is in scope or the institute
/// has no custom branding configured.
/// </summary>
public class BrandingViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;

    public BrandingViewComponent(AppDbContext db, IInstituteScope scope)
    {
        _db = db;
        _scope = scope;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string? logoUrl = null;
        string? primaryColor = null;
        string instituteName = "AIMBYS";

        try
        {
            var instituteId = await _scope.GetCurrentInstituteIdAsync(UserClaimsPrincipal, HttpContext.RequestAborted);
            if (instituteId.HasValue)
            {
                var inst = await _db.Institutes
                    .AsNoTracking()
                    .Where(i => i.Id == instituteId.Value)
                    .Select(i => new { i.LogoUrl, i.PrimaryColorHex, i.Name })
                    .FirstOrDefaultAsync(HttpContext.RequestAborted);

                if (inst != null)
                {
                    logoUrl = inst.LogoUrl;
                    primaryColor = inst.PrimaryColorHex;
                    instituteName = inst.Name;
                }
            }
        }
        catch
        {
            // Non-critical; fall back to defaults
        }

        return View(new BrandingViewModel(logoUrl, primaryColor, instituteName));
    }
}

public sealed record BrandingViewModel(string? LogoUrl, string? PrimaryColorHex, string InstituteName);
