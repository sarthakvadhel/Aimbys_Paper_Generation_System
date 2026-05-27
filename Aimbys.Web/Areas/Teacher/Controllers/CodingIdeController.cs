using Aimbys.Application.Coding;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.Teacher.Controllers;

/// <summary>
/// Standalone IDE page with Monaco editor for authoring and testing
/// coding questions. Teachers can write code, run sample test cases,
/// and verify expected output before attaching the question to an exam.
/// </summary>
[Area("Teacher")]
[Authorize(Roles = Roles.Teacher)]
public class CodingIdeController : Controller
{
    private readonly ICodeExecutionService _execution;

    public CodingIdeController(ICodeExecutionService execution)
    {
        _execution = execution;
    }

    /// <summary>GET: /Teacher/CodingIde — standalone IDE page.</summary>
    public IActionResult Index() => View();

    /// <summary>
    /// POST: /Teacher/CodingIde/Run — runs sample test cases.
    /// JSON endpoint; CSRF token sent via X-CSRF-TOKEN header by the
    /// Monaco IDE client script.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Run([FromBody] CodeRunRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { error = "Source code is required." });
        }

        var result = await _execution.RunSampleAsync(request, ct);
        return Json(result);
    }
}
