---
inclusion: always
---

# Aimbys Paper Generation System — project conventions

This is an enterprise examination + paper-management platform. **Do not** treat
it as a green-field skeleton; the architecture is already in place.

## Solution layout

```
Aimbys.Domain          // entities, enums, permissions, soft-delete contracts
Aimbys.Application     // service interfaces + DTOs (no EF, no MVC)
Aimbys.Infrastructure  // EF Core, AppDbContext, service implementations, identity
Aimbys.Web             // ASP.NET Core MVC: Controllers, Areas, Views, ViewComponents
```

When adding a service: interface + DTOs in
`Aimbys.Application/<Folder>/I<Name>.cs`, implementation in
`Aimbys.Infrastructure/<Folder>/<Name>.cs`, registered as **Scoped** in
`Aimbys.Infrastructure/DependencyInjection.cs`.

## Identity model

Only **four** Identity roles exist (constants in
`Aimbys.Infrastructure.Identity.Roles`):

- `SuperAdmin` — platform-wide
- `InstituteAdmin` — tenant-wide
- `Teacher` — base teacher access
- `Student`

"Evaluator", "Moderator", "Reviewer", "Proctor" are **not** roles. They are
the 13 boolean flags on `TeacherProfile`, with constants in
`Aimbys.Domain.Permissions.TeacherPermissions`. Check them via
`IPermissionGuard.HasAsync(...)` or `[RequiresPermission(TeacherPermissions.CanX)]`.
**Never** use `User.IsInRole("Evaluator")` and **never** read teacher flags
directly from a controller.

## Tenancy

Every tenant-scoped query must filter by `InstituteId`. Resolve the current
tenant via `IInstituteScope.GetCurrentInstituteIdAsync(User, ct)` — it returns
`null` for `SuperAdmin` (cross-tenant) and for users without a profile.
Controllers must `return Forbid()` when an institute-scoped action gets `null`
back.

## Workflow & status transitions

The platform has a real workflow engine: `WorkflowDefinition`,
`WorkflowInstance`, `WorkflowTransition`, `ApprovalQueue`, `ModerationQueue`,
`ReviewerAssignment`, `WorkflowEscalationRule`. Lifecycle changes
(Paper / Evaluation / Result) **must** flow through `IWorkflowService`, not
through ad-hoc enum updates in controllers.

Canonical paper states: `Draft → SubmittedForApproval → Approved | Returned →
Published → Archived`. Use the `PaperStatus` enum; never hardcode strings.

## Audit + notifications + soft delete

- Every workflow transition / approval / publication / suspicious event must
  call `IAuditWriter`. Do not call `_db.AuditLogs.Add(...)` directly.
- User-visible events use `INotificationService` + an `INotificationProjection`
  (one per `IDomainEvent`), not direct rows on `Notifications`.
- Domain entities implementing `ISoftDelete` must be removed via
  `ISoftDeleteService`. Hard delete is reserved for SuperAdmin governance.

## Controllers

- Always `[Authorize(Roles = ...)]`. Use the `Roles` constants.
- Always async + `CancellationToken`.
- Always `AsNoTracking()` on read-only queries.
- Never put business logic in a controller — call a service.
- Never write to `AppDbContext` from a controller for cross-cutting concerns
  (audit, notifications, workflow state) — use the dedicated service.

## Views

- Bootstrap 5, no Tailwind, no client-side framework. Server-rendered Razor.
- Reuse the existing primitives: `KpiCard` and `ChartCard` view components,
  `_DataTable` partial, `_StatusBadge` partial, `StatusBadge.Render(...)` for
  inline cells.
- Layout is `_RoleLayout.cshtml` — keep the dark-sidebar enterprise theme.
- KPI / table / chart endpoints follow the same shape as
  `IDashboardService.Get*SnapshotAsync` — single read service, immutable
  record DTO, controller is thin.

## Charts

Chart endpoints return `{ labels, datasets: [{label, data, backgroundColor?}, ...] }`
JSON to match the existing `wwwroot/js/charts.js` Chart.js helper. Use a
`ChartFeed` (`Labels` + `IReadOnlyList<ChartSeries>`) on the service side and
reshape in the controller.

## EF Core notes

- Bucketed time series (hourly / daily): pull raw timestamps in the window via
  EF (small, indexed columns) and bucket in memory. Don't rely on EF
  translation of SQL date functions.
- DbSet names: `Institutes`, `TeacherProfiles`, `StudentProfiles`, `Subjects`,
  `ClassBatches`, `Questions`, `Papers`, `PaperVersions`, `PaperQuestions`,
  `Exams`, `ExamAttempts`, `ExamAttemptAnswers`, `Results`, `Evaluations`,
  `Notifications`, `AuditLogs`, `ApprovalQueues`, `ModerationQueues`,
  `WorkflowInstances`, `ScheduledJobs`. **Note**: `_db.ModerationRecords` (not
  `Moderations`).
- `Result.IsPublished + PublishedAtUtc` — use both for "published results"
  filters.
- `ExamAttempt` has both `StudentUserId` (Identity user id) and
  `StudentProfileId` (FK). Use the right one for the query.
- `Paper.AuthorTeacherProfileId` is the FK; `Paper.CurrentVersionId` points
  at the active `PaperVersion` (which carries `Title`, `TotalMarks`,
  `DurationMinutes`).

## What not to do

- Do **not** add new top-level dependencies (Hangfire, Quartz, MediatR,
  FluentValidation, AutoMapper) without explicit confirmation. The repo
  deliberately uses none of these.
- Do **not** introduce magic strings for status transitions.
- Do **not** ship hardcoded mock data on dashboards or list pages — return
  guided empty states instead.
- Do **not** create README files unless asked.

## Working in the sandbox

- The sandbox **does not** have a .NET 10 SDK installed; you cannot build
  or run migrations from here. Verify code by reading + reasoning, push to
  branch / main, and let CI / the user's local environment compile.
