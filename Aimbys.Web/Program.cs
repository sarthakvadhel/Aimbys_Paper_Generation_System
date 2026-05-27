using System.Threading.RateLimiting;
using Aimbys.Application.Scheduling;
using Aimbys.Infrastructure;
using Aimbys.Infrastructure.Exams;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Questions;
using Aimbys.Infrastructure.Retention;
using Aimbys.Infrastructure.Storage;
using Aimbys.Web.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── Rate Limiting (Chunk 38) ──────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 5 per minute per IP
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Export endpoints: 10 per minute per user
    options.AddFixedWindowLimiter("export", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Bulk operations: 2 per minute per user
    options.AddFixedWindowLimiter("bulk", opt =>
    {
        opt.PermitLimit = 2;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Heartbeat: 1 per 5 seconds per user
    options.AddFixedWindowLimiter("heartbeat", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromSeconds(5);
        opt.QueueLimit = 0;
    });
});

// ── Output Caching (Chunk 38) ────────────────────────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.NoCache());
    options.AddPolicy("LandingPage", b => b.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("LoginPage", b => b.Expire(TimeSpan.FromMinutes(1)).SetVaryByQuery("ReturnUrl"));
});

// Add services to the container.
//
// `AutoValidateAntiforgeryToken` is registered globally so every unsafe
// HTTP method (POST/PUT/DELETE/PATCH) requires a valid anti-forgery token.
// A controller can opt out per-action with [IgnoreAntiforgeryToken] when
// genuinely needed (e.g. external webhooks).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
});

// EF Core (SQL Server) baseline + ASP.NET Core Identity (with role support).
// The DbContext is registered even when no connection string is set so the
// host can still start; database calls fail at runtime with a SqlException
// pointing the operator at appsettings or user-secrets. See README
// "EF Core / SQL Server setup" and "Identity / first admin user" for local
// configuration.
builder.Services.AddInfrastructure(builder.Configuration);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(DependencyInjection.ConnectionStringName)))
{
    var bootLogger = LoggerFactory
        .Create(b => b.AddConfiguration(builder.Configuration.GetSection("Logging")).AddConsole())
        .CreateLogger("Aimbys.Web.Bootstrap");
    bootLogger.LogWarning(
        "ConnectionStrings:{Name} is not configured. The app will start, but any database call will fail. " +
        "Run `dotnet user-secrets set ConnectionStrings:{Name} \"<your-sqlserver-conn-str>\"` in Aimbys.Web to fix.",
        DependencyInjection.ConnectionStringName,
        DependencyInjection.ConnectionStringName);
}

var app = builder.Build();

// ── Security headers (Chunk 38) — earliest possible so even error pages carry them.
app.UseSecureHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ── Output caching (Chunk 38) — after routing so endpoint metadata is resolved.
app.UseOutputCache();

// ── Rate limiting (Chunk 38) — after routing, before auth.
app.UseRateLimiter();

// Request-metrics middleware (Chunk 35): captures status code and
// elapsed milliseconds for every request and feeds the in-memory
// RequestMetricsCollector that powers the SuperAdmin SystemHealth
// dashboard. Sits right after UseRouting so endpoint metadata is
// available but before authn/authz so 401/403/redirect responses
// are also counted.
app.UseMiddleware<Aimbys.Infrastructure.SystemHealth.RequestMetricsMiddleware>();

// ── Content Security Policy (Chunk 38) — before auth so CSP applies to all responses.
app.UseContentSecurityPolicy();

// Authentication MUST come before Authorization. Identity wires the cookie
// scheme into the request via UseAuthentication.
app.UseAuthentication();
app.UseAuthorization();

// Subscription enforcement runs after auth so we know who the user is and
// can resolve their tenant. Placed before route mapping so a blocked tenant
// short-circuits to the suspension page instead of executing controller code.
app.UseSubscriptionEnforcement();

app.MapStaticAssets();

// Areas first so {area:exists} wins over the default route for /Admin/*.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Idempotent role + (optional) admin user seed. Does NOT throw if the
// database is unreachable so the host can still serve the login page during
// a DB outage; see IdentitySeeder for details.
using (var scope = app.Services.CreateScope())
{
    // File-storage area folders are created at startup so the first upload
    // never has to race directory creation. Idempotent.
    var storageOptions = scope.ServiceProvider
        .GetRequiredService<IOptions<LocalFileStorageOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(storageOptions.RootPath))
    {
        FileFolders.EnsureCreated(storageOptions.RootPath);
    }

    await IdentitySeeder.SeedAsync(scope.ServiceProvider);

    // Register the recurring retention-enforcement job. Idempotent &mdash;
    // ScheduleRecurringAsync upserts on JobKey, so restarts don't multiply
    // rows. Wrapped in a try/catch so a transient DB outage doesn't take
    // the host down; SchedulingHostedService logs that the job is missing
    // until the next successful registration on a later restart.
    try
    {
        var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulingService>();
        await scheduler.ScheduleRecurringAsync(
            RetentionEnforcementJobHandler.Key,
            RetentionEnforcementJobHandler.DefaultCron);
        await scheduler.ScheduleRecurringAsync(
            QuestionAnalyticsAggregator.Key,
            QuestionAnalyticsAggregator.DefaultCron);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Aimbys.Web.Bootstrap");
        logger.LogWarning(ex,
            "Failed to register the recurring retention-enforcement job at startup. "
          + "It will be retried on the next restart.");
    }
}

app.Run();
