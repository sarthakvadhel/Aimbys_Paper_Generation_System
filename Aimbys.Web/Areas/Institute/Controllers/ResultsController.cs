using Aimbys.Application.Results;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Institute.Controllers;

/// <summary>
/// Institute-level result publication and archive management.
/// </summary>
[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class ResultsController : Controller
{
    private readonly IResultPublicationService _publication;

    public ResultsController(IResultPublicationService publication)
    {
        _publication = publication;
    }

    /// <summary>Exam list with publication status.</summary>
    public IActionResult Index() => View();

    /// <summary>Publish results for a given exam.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid examId, CancellationToken ct)
    {
        var result = await _publication.PublishAsync(examId, User, ct);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Results published for {result.StudentsPublished} student(s).";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Archive download links for an exam.</summary>
    public IActionResult Archives() => View();
}
