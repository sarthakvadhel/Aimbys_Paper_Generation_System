using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Identity;
using Aimbys.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Controllers;

/// <summary>
/// Minimal MVC-based account controller. Replaces the Razor-Pages-based
/// Identity Default UI to keep the surface area small and the views
/// consistent with the rest of the MVC app.
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// TempData key used to surface a flash message on the landing page
    /// when an authenticated user has no canonical role assigned yet.
    /// Picked up by <c>Views/Home/Index.cshtml</c>.
    /// </summary>
    public const string NoRoleFlashKey = "NoRoleFlash";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly AppDbContext _db;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<AccountController> logger,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _db = db;
    }

    // ---------- Login ----------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Rate-limiting on the login POST is a future hardening item
        // (see Chunk 38 — "Login throttle / brute-force protection").
        // The hook lives here so reviewers can find the right place
        // when the time comes.

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in.", model.Email);
            return await ResolvePostLoginRedirectAsync(model.Email, model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            return View(model);
        }

        // Don't disclose whether the email exists.
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    // ---------- Register -------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // New self-registered users land with no role by default. In the
        // PARAKH model, an Institute Admin invites them and assigns the
        // appropriate role + permission flags (Chunk 17). Until then they
        // can sign in but every role-area URL (/SuperAdmin, /Institute,
        // /Teacher, /Student) will deny them.

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New user {Email} registered.", model.Email);

        return await ResolvePostLoginRedirectAsync(model.Email, model.ReturnUrl);
    }

    // ---------- Logout ---------------------------------------------------

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out.");
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    // ---------- Change Password ------------------------------------------

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        // Clear the MustChangePassword flag
        var policy = await _db.UserPasswordPolicies.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (policy is not null)
        {
            policy.MustChangePassword = false;
            policy.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Password changed successfully.";

        // Redirect to role home
        var roles = await _userManager.GetRolesAsync(user);
        return Redirect(ResolveHomePathForRoles(roles));
    }

    // ---------- Access denied -------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------- Helpers --------------------------------------------------

    /// <summary>
    /// Decides where to send the user after a successful sign-in:
    ///
    /// <list type="number">
    ///   <item>If <paramref name="returnUrl"/> is a local URL, honour it
    ///         (existing UX preserved: a deep-link followed by a login
    ///         lands back on the deep-link).</item>
    ///   <item>Otherwise route to the user's role home via
    ///         <see cref="RoleHomeRedirector"/>.</item>
    ///   <item>If the user has no canonical role yet, drop a TempData
    ///         flash so the landing page can render the
    ///         "no access" banner described in the spec.</item>
    /// </list>
    /// </summary>
    private async Task<IActionResult> ResolvePostLoginRedirectAsync(string email, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Fetch the user (already-signed-in HttpContext.User isn't yet
        // populated on the same request — a SignInManager quirk).
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var path = ResolveHomePathForRoles(roles);

        // Force password change if flagged
        var policy = await _db.UserPasswordPolicies
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (policy?.MustChangePassword == true)
            return RedirectToAction(nameof(ChangePassword));

        if (path == RoleHomeRedirector.FallbackHome)
        {
            TempData[NoRoleFlashKey] =
                "Your account doesn't have a workspace yet. Please ask your "
              + "Institute Admin to assign a role.";
        }

        return Redirect(path);
    }

    /// <summary>
    /// Mirrors <see cref="RoleHomeRedirector.GetHomePath"/> but operates
    /// on the freshly-fetched role list rather than a
    /// <see cref="System.Security.Claims.ClaimsPrincipal"/> &mdash; which
    /// the post-login round-trip doesn't yet have.
    /// </summary>
    private static string ResolveHomePathForRoles(IList<string> roles)
    {
        if (roles.Contains(Roles.SuperAdmin))     return "/SuperAdmin";
        if (roles.Contains(Roles.InstituteAdmin)) return "/Institute";
        if (roles.Contains(Roles.Teacher))        return "/Teacher";
        if (roles.Contains(Roles.Student))        return "/Student";
        return RoleHomeRedirector.FallbackHome;
    }
}
