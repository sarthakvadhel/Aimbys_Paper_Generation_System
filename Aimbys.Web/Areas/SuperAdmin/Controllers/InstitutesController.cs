using System.Security.Claims;
using System.Text;
using Aimbys.Application.Audit;
using Aimbys.Application.Institutes;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Models.UI;
using Aimbys.Web.ViewModels.Institutes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

//[Route("{Area}/{controller}/{action}")]
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class InstitutesController : Controller
{
    private const int PageSize = 20;

    private readonly AppDbContext _db;
    private readonly IInstituteOnboardingService _onboarding;
    private readonly IAuditWriter _audit;
    private readonly IAntiforgery _antiforgery;

    public InstitutesController(AppDbContext db, IInstituteOnboardingService onboarding, IAuditWriter audit, IAntiforgery antiforgery)
    {
        _db = db;
        _onboarding = onboarding;
        _audit = audit;
        _antiforgery = antiforgery;
    }

    public async Task<IActionResult> Index(string? q, string? status, string? type, int page = 1)
    {
        var query = _db.Institutes.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(search) || i.Code.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InstituteStatus>(status, true, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<InstituteType>(type, true, out var typeEnum))
        {
            query = query.Where(i => i.Type == typeEnum);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var institutes = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var columns = new[]
        {
            new DataTableColumn("Code"),
            new DataTableColumn("Name"),
            new DataTableColumn("City / State"),
            new DataTableColumn("Type"),
            new DataTableColumn("Status"),
            new DataTableColumn("License"),
            new DataTableColumn("Actions")
        };

        var rows = institutes.Select(i =>
        {
            var statusBadge = StatusBadge.Render(i.Status.ToString());
            var actions = BuildActionButtons(i.Id, i.Status);
            return new DataTableRow(new DataTableCell[]
            {
                new(i.Code),
                new(i.Name),
                new($"{i.City}, {i.State}"),
                new(i.Type.ToString()),
                new(statusBadge, IsHtml: true),
                new(i.LicenseTier.ToString()),
                new(actions, IsHtml: true)
            });
        }).ToList();

        var statusOptions = Enum.GetValues<InstituteStatus>()
            .Select(s => new FilterBarOption(s.ToString(), s.ToString()))
            .ToList();
        var typeOptions = Enum.GetValues<InstituteType>()
            .Select(t => new FilterBarOption(t.ToString(), t.ToString()))
            .ToList();

        var model = new InstituteIndexViewModel
        {
            Table = new DataTableModel("Institutes", columns, rows),
            FilterBar = new FilterBarModel(
                SearchName: "q",
                SearchValue: q,
                SearchPlaceholder: "Search by name or code...",
                Filters: new[]
                {
                    new FilterBarSelect("Status", "status", status, statusOptions),
                    new FilterBarSelect("Type", "type", type, typeOptions)
                }),
            Pagination = new PaginationModel(page, totalPages)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institute is null)
            return NotFound();

        var transitions = await _db.WorkflowTransitions
            .Where(t => t.Instance!.SubjectType == "Institute" && t.Instance.SubjectId == id)
            .OrderByDescending(t => t.TransitionedAtUtc)
            .ToListAsync();

        ViewBag.Transitions = transitions;
        return View(institute);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InstituteCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var request = new InstituteCreateRequest(
            Name: model.Name,
            Code: model.Code,
            Type: model.Type,
            City: model.City,
            State: model.State,
            Country: model.Country,
            ContactEmail: model.ContactEmail,
            ContactPhone: model.ContactPhone,
            LicenseTier: model.LicenseTier,
            AdminEmail: model.AdminEmail);

        var result = await _onboarding.CreateAsync(request, User, CancellationToken.None);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create institute.");
            return View(model);
        }

        TempData["Success"] = "Institute created successfully and is pending approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _onboarding.ApproveAsync(id, User, CancellationToken.None);
        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage;
        else
            TempData["Success"] = "Institute approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? reason)
    {
        var result = await _onboarding.RejectAsync(id, User, reason, CancellationToken.None);
        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage;
        else
            TempData["Success"] = "Institute rejected.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Reason is required for suspension.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _onboarding.SuspendAsync(id, User, reason, CancellationToken.None);
        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage;
        else
            TempData["Success"] = "Institute suspended.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var result = await _onboarding.ReactivateAsync(id, User, CancellationToken.None);
        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage;
        else
            TempData["Success"] = "Institute reactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? status, string? type)
    {
        var query = _db.Institutes.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InstituteStatus>(status, true, out var statusEnum))
            query = query.Where(i => i.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<InstituteType>(type, true, out var typeEnum))
            query = query.Where(i => i.Type == typeEnum);

        var institutes = await query.OrderBy(i => i.Code).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Code,Name,City,State,Type,Status,LicenseTier,ContactEmail,CreatedAtUtc");
        foreach (var i in institutes)
        {
            sb.AppendLine($"\"{Escape(i.Code)}\",\"{Escape(i.Name)}\",\"{Escape(i.City)}\",\"{Escape(i.State)}\",\"{i.Type}\",\"{i.Status}\",\"{i.LicenseTier}\",\"{Escape(i.ContactEmail)}\",\"{i.CreatedAtUtc:O}\"");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _audit.WriteAsync(
            "Institutes.Exported",
            "Institute",
            "all",
            userId,
            $"{{\"count\":{institutes.Count}}}",
            AuditSeverity.Information,
            HttpContext.RequestAborted);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"institutes-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Escape(string value) => value.Replace("\"", "\"\"");

    private string BuildActionButtons(Guid id, InstituteStatus status)
    {
        var token = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;
        var tokenInput = $"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{token}\" />";

        var sb = new StringBuilder();
        sb.Append("<div class=\"d-flex gap-1 flex-wrap\">");

        if (status == InstituteStatus.PendingApproval)
        {
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Approve", "Institutes", new { area = "SuperAdmin", id })}\">{tokenInput}<button type=\"submit\" class=\"btn btn-success btn-sm\">Approve</button></form>");
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Reject", "Institutes", new { area = "SuperAdmin", id })}\">{tokenInput}<button type=\"submit\" class=\"btn btn-danger btn-sm\">Reject</button></form>");
        }
        else if (status == InstituteStatus.Active)
        {
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Suspend", "Institutes", new { area = "SuperAdmin", id })}\">{tokenInput}<input type=\"hidden\" name=\"reason\" value=\"Administrative action\" /><button type=\"submit\" class=\"btn btn-warning btn-sm\">Suspend</button></form>");
        }
        else if (status == InstituteStatus.Suspended)
        {
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Reactivate", "Institutes", new { area = "SuperAdmin", id })}\">{tokenInput}<button type=\"submit\" class=\"btn btn-primary btn-sm\">Reactivate</button></form>");
        }

        sb.Append($" <a href=\"{Url.Action("Details", "Institutes", new { area = "SuperAdmin", id })}\" class=\"btn btn-outline-secondary btn-sm\">Details</a>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
