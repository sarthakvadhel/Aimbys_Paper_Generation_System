using Aimbys.Application.Analytics;
using Aimbys.Application.Dashboard;
using Aimbys.Application.Papers;
using Aimbys.Application.Blueprints;
using Aimbys.Application.OrgTree;
using Aimbys.Application.Authorization;
using Aimbys.Application.Audit;
using Aimbys.Application.Broadcasts;
using Aimbys.Application.Bulk;
using Aimbys.Application.Configuration;
using Aimbys.Application.DocumentRendering;
using Aimbys.Application.Evaluation;
using Aimbys.Application.Exams;
using Aimbys.Application.Institutes;
using Aimbys.Application.Moderation;
using Aimbys.Application.Notifications;
using Aimbys.Application.Notifications.Projections;
using Aimbys.Application.Questions;
using Aimbys.Application.Results;
using Aimbys.Application.Scheduling;
using Aimbys.Application.SoftDelete;
using Aimbys.Application.SystemHealth;
using Aimbys.Application.Users;
using Aimbys.Application.Verification;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Events;
using Aimbys.Application.Storage;
using Aimbys.Infrastructure.Blueprints;
using Aimbys.Infrastructure.OrgTree;
using Aimbys.Infrastructure.Analytics;
using Aimbys.Infrastructure.Audit;
using Aimbys.Infrastructure.Authorization;
using Aimbys.Infrastructure.Broadcasts;
using Aimbys.Infrastructure.Bulk;
using Aimbys.Infrastructure.Configuration;
using Aimbys.Infrastructure.Dashboard;
using Aimbys.Application.Subscriptions;
using Aimbys.Infrastructure.Subscriptions;
using Aimbys.Infrastructure.DocumentRendering;
using Aimbys.Infrastructure.Evaluation;
using Aimbys.Infrastructure.Exams;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Institutes;
using Aimbys.Infrastructure.Moderation;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Papers;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Infrastructure.Questions;
using Aimbys.Infrastructure.Results;
using Aimbys.Infrastructure.Retention;
using Aimbys.Infrastructure.Scheduling;
using Aimbys.Infrastructure.SoftDelete;
using Aimbys.Infrastructure.Storage;
using Aimbys.Infrastructure.SystemHealth;
using Aimbys.Infrastructure.Users;
using Aimbys.Infrastructure.Verification;
using Aimbys.Infrastructure.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications = Aimbys.Infrastructure.Notifications;
using Projections = Aimbys.Application.Notifications.Projections;
using Microsoft.Extensions.Hosting;

namespace Aimbys.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configuration key used for the SQL Server connection string.
    /// </summary>
    public const string ConnectionStringName = "Default";

    /// <summary>
    /// Registers the <see cref="AppDbContext"/> against SQL Server using the
    /// connection string at <c>ConnectionStrings:Default</c>, plus ASP.NET
    /// Core Identity (with role support) backed by the same context.
    ///
    /// The DbContext is registered even when the connection string is empty so
    /// that the Web host can start without a database. Calls into the context
    /// will fail at runtime with a clear SqlException describing the missing
    /// configuration.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString ?? string.Empty,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        services
            .AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                // Sensible local-dev defaults; tighten in production via config.
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.Name = "Aimbys.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        });

        // Bind admin-seed configuration so the seeder can opt in cleanly.
        services.Configure<DefaultAdminOptions>(
            configuration.GetSection(DefaultAdminOptions.SectionName));

        // Permission guard: the only sanctioned route for checking teacher
        // permission flags. Scoped to align with AppDbContext + UserManager.
        services.AddScoped<IPermissionGuard, PermissionGuard>();

        // Domain events + notifications (Chunk 10).
        services.AddScoped<Notifications.DomainEventCollector>();
        services.AddScoped<IDomainEventDispatcher, Notifications.DomainEventDispatcher>();
        services.AddScoped<INotificationService, Notifications.NotificationService>();

        // ----- Notification channels (Chunk 13) -------------------------
        // The in-app channel persists to the Notifications table and
        // honours user preferences; the logging channel is a dev/staging
        // catch-all kept from Chunk 10. Order matters only for the
        // log output; the dispatcher fans out to every registered
        // channel after applying user-preference filtering.
        services.AddScoped<INotificationChannel, Notifications.InAppNotificationChannel>();
        services.AddSingleton<INotificationChannel, Notifications.LoggingNotificationChannel>();

        // ----- Notification templates + preferences (Chunk 13) ----------
        services.AddScoped<INotificationTemplateService, Notifications.NotificationTemplateService>();
        services.AddScoped<INotificationPreferenceService, Notifications.NotificationPreferenceService>();

        // Register all 8 notification projections.
        services.AddScoped<INotificationProjection<PaperSubmittedEvent>, Projections.PaperSubmittedProjection>();
        services.AddScoped<INotificationProjection<PaperApprovedEvent>, Projections.PaperApprovedProjection>();
        // Slice B: Returned/Published/Archived projections complete the
        // paper lifecycle so the author is notified at every stage.
        services.AddScoped<INotificationProjection<PaperReturnedEvent>, Projections.PaperReturnedProjection>();
        services.AddScoped<INotificationProjection<PaperPublishedEvent>, Projections.PaperPublishedProjection>();
        services.AddScoped<INotificationProjection<PaperArchivedEvent>, Projections.PaperArchivedProjection>();
        services.AddScoped<INotificationProjection<EvaluationAssignedEvent>, Projections.EvaluationAssignedProjection>();
        services.AddScoped<INotificationProjection<ModerationReturnedEvent>, Projections.ModerationReturnedProjection>();
        services.AddScoped<INotificationProjection<ExamScheduledEvent>, Projections.ExamScheduledProjection>();
        services.AddScoped<INotificationProjection<ResultPublishedEvent>, Projections.ResultPublishedProjection>();
        services.AddScoped<INotificationProjection<InstituteApprovedEvent>, Projections.InstituteApprovedProjection>();
        services.AddScoped<INotificationProjection<UserSuspendedEvent>, Projections.UserSuspendedProjection>();
        // Tenancy resolver. Scoped because it queries AppDbContext.
        services.AddScoped<IInstituteScope, InstituteScope>();

        // Local file storage. Bind options from `FileStorage` section and
        // default RootPath to `<ContentRoot>/uploads` when unset so a fresh
        // checkout works without configuration.
        services
            .AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection(LocalFileStorageOptions.SectionName))
            .PostConfigure<IHostEnvironment>((opts, env) =>
            {
                if (string.IsNullOrWhiteSpace(opts.RootPath))
                {
                    opts.RootPath = Path.Combine(env.ContentRootPath, "uploads");
                }
            });

        // The single concrete is registered once and exposed under both the
        // generic and local-specific interfaces so consumers get the same
        // scoped instance regardless of which they injected.
        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<ILocalFileStorageService>(sp => sp.GetRequiredService<LocalFileStorageService>());
        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<LocalFileStorageService>());

        // ----- Audit (Chunk 11) -----------------------------------------
        // Single sanctioned route for writing AuditLog rows. Existing
        // controllers that still call `_db.AuditLogs.Add(...)` directly
        // will be migrated to this interface in follow-up chunks.
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditWriter, AuditWriter>();

        // ----- Workflow engine (Chunk 11) -------------------------------
        // The definition registry is a Singleton: the embedded JSON specs
        // are immutable for the lifetime of the process. The engine and
        // escalation service are Scoped so they share the request-scoped
        // AppDbContext and DomainEventCollector.
        services.AddSingleton<IWorkflowDefinitionRegistry, WorkflowDefinitionRegistry>();
        services.AddScoped<IWorkflowService, WorkflowEngine>();
        services.AddScoped<IWorkflowEscalationService, WorkflowEscalationService>();

        // ----- Enterprise infrastructure (Chunk 12) ---------------------
        // IMemoryCache is the backing store for IConfigurationService's
        // 5-minute read cache. AddMemoryCache is idempotent so calling
        // it from multiple composition roots is safe.
        services.AddMemoryCache();

        // Soft-delete: ISoftDeleteService scoped to the request unit-of-work.
        services.AddScoped<ISoftDeleteService, SoftDeleteService>();

        // Scheduling: SchedulingService is scoped (uses AppDbContext);
        // SchedulingHostedService runs as a singleton BackgroundService
        // and creates per-tick scopes itself.
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddHostedService<SchedulingHostedService>();

        // Each IScheduledJobHandler is scoped (consumes AppDbContext +
        // IAuditWriter). Resolved by JobKey at dispatch time inside the
        // hosted service's per-tick scope.
        services.AddScoped<IScheduledJobHandler, RetentionEnforcementJobHandler>();

        // Document rendering: stub HTML-to-PDF converter; production
        // deployments replace it with a real adapter (Puppeteer,
        // wkhtmltopdf, QuestPDF) without touching the consumer.
        services.AddScoped<IHtmlToPdfConverter, LoggingHtmlToPdfConverter>();
        services.AddScoped<IDocumentRenderService, DocumentRenderService>();

        // Bulk operations: full impls of ImportStudentsAsync /
        // ActivateDeactivateBulkAsync / NotifyBulkAsync ship in V1; the
        // remaining methods are stubs until their underlying aggregates
        // (Exam, Result, Paper) land.
        services.AddScoped<IBulkOperationService, BulkOperationService>();

        // Central configuration: cached behind IMemoryCache; writes
        // invalidate the matching cache key.
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // ----- Audit visibility (Chunk 13) ------------------------------
        // Filters audit-log rows by role / permission / compliance gate
        // and masks sensitive fields in DetailsJson. Called by the
        // (future) audit viewer; cached behind IMemoryCache for the
        // rule set.
        services.AddScoped<IAuditVisibilityService, Aimbys.Infrastructure.Audit.AuditVisibilityService>();

        // ----- Org tree (Chunk 18) --------------------------------------
        services.AddScoped<IOrgTreeService, OrgTreeService>();
        // ----- Institute onboarding (Chunk 17) --------------------------
        services.AddScoped<IInstituteOnboardingService, InstituteOnboardingService>();

        // ----- Exam services (Chunk 27) --------------------------------
        services.AddScoped<IExamSchedulingService, ExamSchedulingService>();
        services.AddScoped<IExamRuntimeService, ExamRuntimeService>();
        services.AddScoped<IExamSecurityService, ExamSecurityService>();
        services.AddScoped<IScheduledJobHandler, ExamAutoSubmitJobHandler>();

        // ----- Evaluation (Chunk 28) ------------------------------------
        services.AddScoped<IEvaluationService, EvaluationService>();
        services.AddScoped<IEvaluationAssignmentService, EvaluationAssignmentService>();

        // ----- Moderation (Chunk 29) ------------------------------------
        services.AddScoped<IModerationService, ModerationService>();

        // ----- Results (Chunk 29) ---------------------------------------
        services.AddScoped<IResultPublicationService, ResultPublicationService>();
        services.AddScoped<IAppealService, AppealService>();

        // ----- Analytics (Chunk 30) -------------------------------------
        services.AddScoped<IAnalyticsAggregationService, AnalyticsAggregationService>();

        // ----- Dashboard aggregation (Slice A) --------------------------
        // Single read-only service consumed by the four role HomeControllers.
        // All queries use AsNoTracking; tenancy is the caller's responsibility
        // (resolved via IInstituteScope before this service is invoked).
        services.AddScoped<IDashboardService, DashboardService>();

        // ----- Paper assembly (Chunk 22) --------------------------------
        services.AddScoped<IPaperValidationService, PaperValidationService>();
        services.AddScoped<IPaperAssemblyService, PaperAssemblyService>();

        // ----- Question lifecycle (Chunk 21) ----------------------------
        services.AddScoped<IQuestionLifecycleService, Aimbys.Infrastructure.Questions.QuestionLifecycleService>();
        // ----- Question authoring (Chunk 20) ----------------------------
        services.AddScoped<IQuestionAuthoringService, QuestionAuthoringService>();
        // ----- User management (Chunk 19) -------------------------------
        services.AddScoped<IUserManagementService, UserManagementService>();

        // ----- SuperAdmin governance (Chunk 35) -------------------------
        // RequestMetricsCollector is a singleton: one rolling 24h
        // window per process, shared by the metrics middleware and the
        // SystemHealth viewer. BroadcastService is scoped because it
        // touches AppDbContext and IAuditWriter.
        services.AddSingleton<IRequestMetricsCollector, RequestMetricsCollector>();
        services.AddScoped<IBroadcastService, BroadcastService>();

        // ----- Subscription management (Chunk 36) -----------------------
        services.AddScoped<ISubscriptionManagementService, SubscriptionManagementService>();
        services.AddScoped<IScheduledJobHandler, SubscriptionExpirationJobHandler>();

        // ----- Exports + QR + branding (Chunk 39) -----------------------
        services.AddScoped<IPrintLogService, PrintLogService>();
        services.AddScoped<IQrVerificationService, QrVerificationService>();

        return services;
    }
}
