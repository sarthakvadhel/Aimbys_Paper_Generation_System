using Aimbys.Application.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Controllers;

/// <summary>
/// Public verification endpoint for QR-encoded paper tokens.
/// No authentication required — anyone with the QR code can verify.
/// </summary>
public class VerifyController : Controller
{
    private readonly IQrVerificationService _verification;

    public VerifyController(IQrVerificationService verification)
    {
        _verification = verification;
    }

    [AllowAnonymous]
    [HttpGet("verify/paper/{token}")]
    public async Task<IActionResult> Paper(string token, CancellationToken ct)
    {
        var result = await _verification.VerifyAsync(token, ct);
        return View(result);
    }
}
