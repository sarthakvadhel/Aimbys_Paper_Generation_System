using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aimbys.Infrastructure.Identity;

/// <summary>
/// Idempotent seeder for the four canonical Identity roles (and an optional
/// initial Super Admin user). Invoke once at startup from <c>Program.cs</c>
/// via <see cref="SeedAsync"/>.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Ensures every role in <see cref="Roles.All"/> exists and, if
    /// <see cref="DefaultAdminOptions"/> is fully populated, ensures a
    /// matching <see cref="Roles.SuperAdmin"/> user exists.
    ///
    /// Safe to call on every startup. Does not throw if the database is
    /// unreachable &mdash; logs a warning and returns &mdash; so the host can
    /// still boot to serve the login page during a DB outage.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Aimbys.Infrastructure.IdentitySeeder");

        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var roleName in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Seeded role '{Role}'.", roleName);
                    }
                    else
                    {
                        logger.LogError(
                            "Failed to seed role '{Role}': {Errors}",
                            roleName,
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            var admin = services.GetRequiredService<IOptions<DefaultAdminOptions>>().Value;
            if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
            {
                logger.LogInformation(
                    "No Identity:DefaultAdmin configured; skipping super-admin user seed. " +
                    "Set Identity:DefaultAdmin:Email and Identity:DefaultAdmin:Password (e.g. via user-secrets) to enable.");
                return;
            }

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var existing = await userManager.FindByEmailAsync(admin.Email);
            if (existing is null)
            {
                var user = new IdentityUser
                {
                    UserName = admin.Email,
                    Email = admin.Email,
                    EmailConfirmed = true
                };
                var create = await userManager.CreateAsync(user, admin.Password);
                if (!create.Succeeded)
                {
                    logger.LogError(
                        "Failed to create default super-admin '{Email}': {Errors}",
                        admin.Email,
                        string.Join("; ", create.Errors.Select(e => e.Description)));
                    return;
                }
                existing = user;
                logger.LogInformation("Seeded default super-admin user '{Email}'.", admin.Email);
            }

            if (!await userManager.IsInRoleAsync(existing, Roles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existing, Roles.SuperAdmin);
                logger.LogInformation("Added '{Email}' to role '{Role}'.", admin.Email, Roles.SuperAdmin);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Identity seed skipped because the database was unreachable. " +
                "Run `dotnet ef database update -p Aimbys.Infrastructure -s Aimbys.Web` to apply migrations.");
        }
    }
}
