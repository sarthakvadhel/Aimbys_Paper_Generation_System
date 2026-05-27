using System.Diagnostics;
using Aimbys.Web.Identity;
using Aimbys.Web.Models;
using Aimbys.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Aimbys.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Public landing page. Authenticated users with a canonical role are
    /// bounced straight to their role home so they never see the
    /// product / role-pick screen again after sign-in (Chunk 14
    /// acceptance criterion: "Authenticated / immediately redirects to
    /// role home").
    ///
    /// <para>
    /// The view is the PARAKH role-pick landing &mdash; it embeds the
    /// login form so an anonymous visitor can sign in without a second
    /// page hop. We pass an empty <see cref="LoginViewModel"/> so the
    /// <c>asp-for</c> tag helpers on the form bind cleanly.
    /// </para>
    /// </summary>
    [AllowAnonymous]
    [OutputCache(PolicyName = "LandingPage")]
    public IActionResult Index(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true
            && RoleHomeRedirector.HasCanonicalRole(User))
        {
            return Redirect(RoleHomeRedirector.GetHomePath(User));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Landing page shown by
    /// <see cref="Aimbys.Web.Middleware.SubscriptionEnforcementMiddleware"/>
    /// when the institute's subscription is in a state that blocks
    /// access (Suspended / Expired / GracePeriod past expiry).
    /// Allows anonymous so the user can read the page after the
    /// authenticated session is interrupted by the redirect.
    /// </summary>
    [AllowAnonymous]
    public IActionResult SubscriptionSuspended()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
