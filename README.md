[![Build](https://github.com/sarthakvadhel/Aimbys-Paper-Generation-Platform/actions/workflows/build.yml/badge.svg)](https://github.com/sarthakvadhel/Aimbys-Paper-Generation-Platform/actions/workflows/build.yml)

# AIMBYS Paper Generation Platform

The current `src/` tree is a Vite + React 18 + Tailwind UI/UX reference, exported
from Figma Make ([original design](https://www.figma.com/design/Al5DYMAGO5w6c2da5A2tBM/Quiz---Q-A-Platform-UI-UX)).
It is being migrated to **ASP.NET Core MVC + Bootstrap 5** (target framework
`net10.0`, SQL Server + EF Core, TinyMCE editor, provider-agnostic generation
abstraction with an OpenAI Responses-style adapter first). The React reference
will be retained until the MVC port reaches feature parity — do not delete it.

## Running the React reference

```sh
npm i
npm run dev      # vite dev server
npm run build    # vite build
```

  ### React UI reference (`src/`)

  Run `npm i` to install the dependencies.

  Run `npm run dev` to start the development server.

  ### ASP.NET Core MVC app (`Aimbys.*` projects)

  This repo also contains an ASP.NET Core MVC skeleton that targets `net10.0`.
  The `.NET 10` SDK version is pinned in `global.json`.

  ```sh
  dotnet restore Aimbys.slnx
  dotnet build   Aimbys.slnx
  dotnet run --project Aimbys.Web/Aimbys.Web.csproj
  ```

  By default the app listens on `http://localhost:5094` (see
  `Aimbys.Web/Properties/launchSettings.json`). Override with
  `ASPNETCORE_URLS=http://127.0.0.1:5117 dotnet run --project Aimbys.Web --no-launch-profile`.

  Solution layout:

  - `Aimbys.Web` — ASP.NET Core MVC entry point (Bootstrap 5 default template).
  - `Aimbys.Application` — application services / use-cases (references `Aimbys.Domain`).
  - `Aimbys.Domain` — domain entities and value objects (no dependencies).
  - `Aimbys.Infrastructure` — persistence / external integrations (references `Aimbys.Domain` and `Aimbys.Application`).

  ### EF Core / SQL Server setup

  The schema is the **PARAKH-aligned organisational foundation**: `Institute`
  (tenancy root), `Department`, `AcademicYear`, `Subject`, `ClassBatch`,
  `TeacherProfile`, `StudentProfile`, plus `AuditLog` and the ASP.NET Identity
  tables. Workflow entities &mdash; Blueprint, AssessmentDesign, Question,
  Paper, Exam, Evaluation, Moderation, Analytics &mdash; arrive in subsequent
  chunks alongside their controllers / views, so the data model and the
  workflows ship as a unit. Mapping lives in `Aimbys.Infrastructure`'s
  `AppDbContext`; migrations live under `Aimbys.Infrastructure/Migrations/`.

  **One-time tooling** (pinned in `dotnet-tools.json`):

  ```sh
  dotnet tool restore
  ```

  **Configure the connection string.** `appsettings.json` ships with an empty
  `ConnectionStrings:Default` so nothing leaks into source control. Pick one:

  - User-secrets (recommended for local dev):

    ```sh
    dotnet user-secrets --project Aimbys.Web set \
      ConnectionStrings:Default \
      "Server=localhost,1433;Database=Aimbys.Dev;User Id=sa;Password=<your-pwd>;Encrypt=True;TrustServerCertificate=True;"
    ```

  - Environment variable (CI / containers):

    ```sh
    export ConnectionStrings__Default="Server=…"
    ```

  - SQL Server LocalDB on Windows: `appsettings.Development.json` already
    points at `(localdb)\\mssqllocaldb` so `dotnet run` just works after
    `sqllocaldb create / start mssqllocaldb`.

  **Apply the schema:**

  ```sh
  dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web
  ```

  **Inspect the generated SQL without applying it:**

  ```sh
  dotnet ef migrations script --project Aimbys.Infrastructure --startup-project Aimbys.Web --idempotent
  ```

  **Add a new migration in the future:**

  ```sh
  dotnet ef migrations add <Name> --project Aimbys.Infrastructure --startup-project Aimbys.Web --output-dir Migrations
  ```

  The Web host starts even when no connection string is configured — it logs a
  warning and only fails on the first DB call. This keeps `dotnet run` useful
  for UI iteration before SQL Server is available.

  ### Identity / first admin user

  ASP.NET Core Identity is wired to `AppDbContext`. Four canonical roles are
  seeded at startup (idempotent): **`SuperAdmin`**, **`InstituteAdmin`**,
  **`Teacher`**, **`Student`**. New users registered via `/Account/Register`
  land with **no role** — in the PARAKH model an Institute Admin invites
  them and assigns the appropriate role + permission flags (Chunk 17). Until
  then a registered user can sign in but every role-area URL denies them.

  Operational capabilities like *Evaluator*, *Moderator*, *Reviewer*,
  *Proctor* are **not** Identity roles. They are the 13 boolean flags on
  `TeacherProfile` (`CanCreateQuestions`, `CanGeneratePaper`,
  `CanManageBlueprints`, `CanEvaluate`, `CanModerate`, `CanPublishResults`,
  `CanScheduleExam`, `CanReviewCodingQuestions`, `CanManageQuestionBank`,
  `CanAssignEvaluators`, `CanManageAnalytics`, `CanApproveQuestions`,
  `CanProctor`), checked by `IPermissionGuard` and the
  `[RequiresPermission("...")]` action filter.

  **Bootstrap a Super Admin via configuration**, *before* the host starts:

  ```sh
  dotnet user-secrets --project Aimbys.Web set Identity:DefaultAdmin:Email "you@example.com"
  dotnet user-secrets --project Aimbys.Web set Identity:DefaultAdmin:Password "ChangeMe!1"
  ```

  On the next `dotnet run`, the seeder creates the user (if missing) and
  ensures it is in the `SuperAdmin` role. The seed is safe to re-run; it
  skips entirely when either key is unset.

  Manual smoke flow once a SQL Server is reachable:

  1. `dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web`
  2. `dotnet run --project Aimbys.Web`
  3. Visit `/Account/Register`, create a user — it lands with no role.
  4. As that user, visit `/Admin` — you should be redirected to
     `/Account/AccessDenied` (HTTP 403).
  5. Sign out, sign in as the seeded `SuperAdmin`, visit `/Admin` — the
     placeholder page renders. (Chunk 13 replaces this with four
     role-specific MVC Areas.)

There are no tests and no CI in this repo today.

---

## React → ASP.NET Core MVC Migration Map

This section is documentation only. No production code is changed by it. Its
purpose is to give every subsequent migration chunk an unambiguous target:
which controller, which action, which view, which view model, which auth
requirement, which forms.

### 1. Audit summary of the React UI

- **Entry**: `src/main.tsx` → `src/app/App.tsx`. The `App` component holds two
  pieces of in-memory state — `role: AppRole | null` and `view: AppView` —
  and conditionally renders one of five components: `LandingPage`,
  `SuperAdminShell`, `InstituteShell`, `TeacherShell`, `StudentShell`.
- **No router**. Despite `react-router` being a dependency, no router is wired.
  Navigation is an internal `setView('xxx')` call. The MVC port replaces this
  with real URLs.
- **Roles** (`AppRole`): `superadmin`, `institute`, `teacher`, `student`.
- **Views** (`AppView`) are prefixed by role: `sa-*`, `inst-*`, `tchr-*`,
  `std-*`, plus `landing`.
- **Exam mode**: when `examActive` is true and `view === 'std-exam'`,
  `ExamInterface` takes over the entire viewport (no shell, no sidebar).
- **Active screen tree** (the only tree to migrate):
  `src/app/components/aimbys/**`.
- **Legacy / unused** (not in scope unless explicitly reinstated by a later
  chunk):
  - `src/app/components/admin/*` (`AdminShell`, `AdminDashboard`,
    `AdminAnalytics`, `QuizManagement`, `QuizUpload`, `UserManagement`).
  - `src/app/components/AuthScreen.tsx`, `src/app/components/Onboarding.tsx`.
  - `src/app/components/figma/ImageWithFallback.tsx` (asset helper).
  - shadcn/ui primitives in `src/app/components/ui/*`.

### 2. URL strategy

Use **MVC Areas** to keep authorization, routing, and view discovery aligned to
roles:

| Area              | URL prefix      | Role required           |
| ----------------- | --------------- | ----------------------- |
| (root)            | `/`             | Anonymous (marketing/auth) |
| `Account`         | `/Account/*`    | Anonymous → authenticated |
| `SuperAdmin`      | `/SuperAdmin/*` | `SuperAdmin`            |
| `Institute`       | `/Institute/*`  | `InstituteAdmin`        |
| `Teacher`         | `/Teacher/*`    | `Teacher` or `Evaluator` |
| `Student`         | `/Student/*`    | `Student`               |

Identity roles to seed: `SuperAdmin`, `InstituteAdmin`, `Teacher`, `Evaluator`,
`Proctor`, `Student`. (`Evaluator` and `Proctor` appear in
`InstituteUsers.tsx` as separate roles.) Apply
`[Authorize(Roles = "...")]` at the controller; layer ownership checks for
teacher-owned questions, student-owned attempts, etc.

### 3. Route-by-route mapping table

`AppView` is the React state value. `Component` is the file under
`src/app/components/aimbys/`. `MVC Route` is the canonical URL. `Controller /
Action` and `View path` are the proposed MVC targets. `Auth` is the role
requirement. `Notes` lists the data the screen reads and any forms / POSTs
discovered in the React source.

#### 3.1 Marketing & authentication (Anonymous)

| AppView   | Component        | MVC Route             | Controller / Action            | View path                              | Auth | Notes |
| --------- | ---------------- | --------------------- | ------------------------------ | -------------------------------------- | ---- | ----- |
| `landing` | `LandingPage`    | `GET /`               | `HomeController.Index`         | `Views/Home/Index.cshtml`              | Anon | Hero, stats, feature list, ISO/CERT-In/TLS footer. Read-only marketing copy. |
| `landing` | `LandingPage` (login form half) | `GET /Account/Login`  | `AccountController.Login`      | `Views/Account/Login.cshtml`           | Anon | Role selector + username + password + "remember device". Submit → `POST /Account/Login` (anti-forgery). On success, redirect to role-specific dashboard. |
| —         | `LandingPage` (login form half) | `POST /Account/Login` | `AccountController.Login`      | redirects                              | Anon | `[ValidateAntiForgeryToken]`. ASP.NET Identity sign-in. Server-side rate limiting + audit log entry per attempt. |
| —         | (new)            | `GET /Account/ForgotPassword` | `AccountController.ForgotPassword` | `Views/Account/ForgotPassword.cshtml` | Anon | Stub. React has a "Forgot password?" link with no handler; reproduce as a placeholder page in chunk 1. |
| —         | (n/a)            | `POST /Account/Logout` | `AccountController.Logout`    | redirects to `/`                       | User | Anti-forgery. Logs audit event. Replaces every shell's sidebar `LogOut` button. |

#### 3.2 Super Admin (`Roles = "SuperAdmin"`, area `SuperAdmin`)

| AppView          | Component              | MVC Route                       | Controller / Action                     | View path                                              | Notes (data + forms) |
| ---------------- | ---------------------- | ------------------------------- | --------------------------------------- | ------------------------------------------------------ | -------------------- |
| `sa-dashboard`   | `SuperAdminDashboard`  | `GET /SuperAdmin`               | `DashboardController.Index`             | `Areas/SuperAdmin/Views/Dashboard/Index.cshtml`        | KPI tiles, platform-activity area chart (24h), regional table, license pie, service status, alerts feed, pending-approvals table with `Approve` / `Review` row actions → `POST /SuperAdmin/Approvals/Approve/{id}`. |
| `sa-institutes`  | `InstituteManagement`  | `GET /SuperAdmin/Institutes`    | `InstitutesController.Index`            | `Areas/SuperAdmin/Views/Institutes/Index.cshtml`       | Search + status + type filters, paginated list. Row actions: view (`GET /SuperAdmin/Institutes/Details/{id}`), edit (`GET /SuperAdmin/Institutes/Edit/{id}` + `POST`), more (suspend, change-license). Toolbar: `Onboard Institute` (`GET /SuperAdmin/Institutes/Create` + `POST`), `Export` (`GET /SuperAdmin/Institutes/Export`). |
| `sa-analytics`   | `GlobalAnalytics`      | `GET /SuperAdmin/Analytics`     | `AnalyticsController.Index`             | `Areas/SuperAdmin/Views/Analytics/Index.cshtml`        | Monthly trend, subject breakdown, question-type radar, pass/fail trend, KPI strip. `Export Report` button → `GET /SuperAdmin/Analytics/Export?format=...`. |
| `sa-licenses`    | (placeholder in shell) | `GET /SuperAdmin/Licenses`      | `LicensesController.Index`              | `Areas/SuperAdmin/Views/Licenses/Index.cshtml`         | Stub for chunk 8 — React shell renders fallback dashboard for this view. |
| `sa-security`    | (placeholder in shell) | `GET /SuperAdmin/Security`      | `SecurityController.Index`              | `Areas/SuperAdmin/Views/Security/Index.cshtml`         | Same — stub. Chunk 8. |
| `sa-system`      | `SystemHealth`         | `GET /SuperAdmin/System`        | `SystemHealthController.Index`          | `Areas/SuperAdmin/Views/SystemHealth/Index.cshtml`     | Infra metrics tiles, CPU/memory + req/error charts (24h), service registry table. Read-only. |
| `sa-audit`       | `AuditLogs`            | `GET /SuperAdmin/Audit`         | `AuditController.Index`                 | `Areas/SuperAdmin/Views/Audit/Index.cshtml`            | Severity + module + free-text filter; paginated table. `Export Logs` → `GET /SuperAdmin/Audit/Export`. Read-only (audit rows are append-only). |
| `sa-broadcasts`  | (placeholder in shell) | `GET /SuperAdmin/Broadcasts`    | `BroadcastsController.Index` / `Create` | `Areas/SuperAdmin/Views/Broadcasts/Index.cshtml`       | Stub. Chunk 8. POST will be `POST /SuperAdmin/Broadcasts/Create` with anti-forgery. |
| —                | derived from dashboard | `POST /SuperAdmin/Approvals/Approve/{id}` / `Reject/{id}` | `ApprovalsController.Approve` / `Reject` | redirects                                            | Anti-forgery; ownership check (the request must belong to a tenant the SuperAdmin governs). |

#### 3.3 Institute Admin (`Roles = "InstituteAdmin"`, area `Institute`)

| AppView              | Component             | MVC Route                              | Controller / Action                | View path                                                | Notes |
| -------------------- | --------------------- | -------------------------------------- | ---------------------------------- | -------------------------------------------------------- | ----- |
| `inst-dashboard`     | `InstituteDashboard`  | `GET /Institute`                       | `DashboardController.Index`        | `Areas/Institute/Views/Dashboard/Index.cshtml`           | KPI tiles, weekly activity area chart, alerts feed, subject performance bars, top contributors table. |
| `inst-users`         | `InstituteUsers`      | `GET /Institute/Users`                 | `UsersController.Index`            | `Areas/Institute/Views/Users/Index.cshtml`               | Role + status + search filters, paginated table. Row actions: view, edit, more. Toolbar `Invite User` → `GET /Institute/Users/Invite` + `POST` (anti-forgery, server validation: email, role, department). |
| `inst-papers`        | (placeholder in shell)| `GET /Institute/Papers`                | `PapersController.Index`           | `Areas/Institute/Views/Papers/Index.cshtml`              | Stub for chunk 4. List of all papers across teachers in the institute with approve/return workflow. |
| `inst-questionbank`  | `QuestionBank`        | `GET /Institute/Questions`             | `QuestionsController.Index`        | `Areas/Institute/Views/Questions/Index.cshtml`           | Type/subject/difficulty filters; cards. Row actions: view (`Details/{id}`), edit (`Edit/{id}` + POST), delete (`POST Delete/{id}`). Toolbar `Add Question` → `Create` (TinyMCE-backed); `Import from Excel` → `POST /Institute/Questions/Import` (`<form enctype="multipart/form-data">`, anti-forgery, file ≤ 10 MB, server validates schema). |
| `inst-calendar`      | `ExamCalendarPage` (inline in shell) | `GET /Institute/Calendar`     | `CalendarController.Index`         | `Areas/Institute/Views/Calendar/Index.cshtml`            | Read-only table; later chunks may add `POST Calendar/Schedule`. |
| `inst-approvals`     | (placeholder in shell)| `GET /Institute/Approvals`             | `ApprovalsController.Index`        | `Areas/Institute/Views/Approvals/Index.cshtml`           | Stub. Workflow inbox for paper approvals from teachers. POSTs: `Approve/{id}`, `Reject/{id}` (anti-forgery, requires comment on reject). |
| `inst-analytics`     | `InstituteAnalytics`  | `GET /Institute/Analytics`             | `AnalyticsController.Index`        | `Areas/Institute/Views/Analytics/Index.cshtml`           | Monthly trend, class-wise bars, subject trend lines, summary table. `Export Report` → `GET /Institute/Analytics/Export`. |
| `inst-settings`      | (placeholder in shell)| `GET /Institute/Settings`              | `SettingsController.Index`         | `Areas/Institute/Views/Settings/Index.cshtml`            | Stub. Branding, academic calendar, integrations. POST per section. |

#### 3.4 Teacher / Examiner (`Roles = "Teacher,Evaluator"`, area `Teacher`)

| AppView              | Component                      | MVC Route                              | Controller / Action                  | View path                                                  | Notes |
| -------------------- | ------------------------------ | -------------------------------------- | ------------------------------------ | ---------------------------------------------------------- | ----- |
| `tchr-dashboard`     | `TeacherDashboard`             | `GET /Teacher`                         | `DashboardController.Index`          | `Areas/Teacher/Views/Dashboard/Index.cshtml`               | KPI tiles, class-avg bar chart, pending evaluations list (deep-link to `/Teacher/Evaluation/{submissionId}`), recent papers table. |
| `tchr-papergen`      | `PaperGeneration`              | `GET /Teacher/Papers`                  | `PapersController.Index`             | `Areas/Teacher/Views/Papers/Index.cshtml`                  | List of teacher's papers. |
| `tchr-papergen` (editor) | `PaperGeneration`          | `GET /Teacher/Papers/Edit/{id}`        | `PapersController.Edit`              | `Areas/Teacher/Views/Papers/Edit.cshtml`                   | Two-pane editor: bank browser (search + multi-select) + paper editor (drag-reorder, remove, totals). Buttons: `Preview` (`GET .../Preview/{id}`), `Save Draft` (`POST /Teacher/Papers/SaveDraft` — anti-forgery, JSON of selected QIDs + ordering + title), `Submit for Approval` (`POST /Teacher/Papers/Submit/{id}`). Title edit is part of the same form. |
| `tchr-blueprint`     | `PaperGeneration` (blueprint tab) | `GET /Teacher/Papers/Blueprint/{id}` | `PapersController.Blueprint`         | `Areas/Teacher/Views/Papers/Blueprint.cshtml`              | Total marks, duration, type-mix, difficulty-mix form. Submit → `POST /Teacher/Papers/GenerateFromBlueprint/{id}` (calls the provider-agnostic generator service). |
| `tchr-questionbank`  | `TeacherQuestionBank`          | `GET /Teacher/Questions`               | `QuestionsController.Index`          | `Areas/Teacher/Views/Questions/Index.cshtml`               | "My Questions" view. CRUD scoped by ownership: server enforces `q.AuthorId == currentUserId` on every Edit/Delete. TinyMCE on Create/Edit. |
| `tchr-evaluation`    | `EvaluationDesk`               | `GET /Teacher/Evaluation`              | `EvaluationController.Index`         | `Areas/Teacher/Views/Evaluation/Index.cshtml`              | Inbox of submissions assigned to this evaluator. |
| `tchr-evaluation` (open submission) | `EvaluationDesk` | `GET /Teacher/Evaluation/{submissionId}` | `EvaluationController.Open`         | `Areas/Teacher/Views/Evaluation/Open.cshtml`               | Split view: student answer + rubric scoring + feedback textarea. Each rubric save: `POST /Teacher/Evaluation/SaveScore` (anti-forgery, JSON: `{submissionId, answerIdx, rubricIdx, points, feedback}`) — autosave per criterion. Final: `POST /Teacher/Evaluation/Submit/{submissionId}`; redirects to inbox. |
| `tchr-reports`       | `TeacherReports` (inline)      | `GET /Teacher/Reports`                 | `ReportsController.Index`            | `Areas/Teacher/Views/Reports/Index.cshtml`                 | Read-only class summary table. |
| `tchr-ide`           | `CodingIDEPage` (inline)       | `GET /Teacher/CodingIDE`               | `CodingIdeController.Index`          | `Areas/Teacher/Views/CodingIde/Index.cshtml`               | Editor + test runner UI. POSTs deferred to a later chunk: `Run` → `POST /Teacher/CodingIDE/Run` (returns test results JSON), `Save to Bank` → `POST /Teacher/CodingIDE/Save` (creates a Coding question owned by current user). Anti-forgery on both. |

#### 3.5 Student / Candidate (`Roles = "Student"`, area `Student`)

| AppView           | Component                    | MVC Route                                                    | Controller / Action                  | View path                                              | Notes |
| ----------------- | ---------------------------- | ------------------------------------------------------------ | ------------------------------------ | ------------------------------------------------------ | ----- |
| `std-dashboard`   | `StudentDashboard`           | `GET /Student`                                               | `DashboardController.Index`          | `Areas/Student/Views/Dashboard/Index.cshtml`           | KPI tiles, next-exam banner with "Start Exam Now" CTA, recent results, subject progress bars. CTA links to `/Student/Exams/Start/{examId}` (POST). |
| `std-exam`        | `ExamLobby` (inline)         | `GET /Student/Exams`                                         | `ExamsController.Index`              | `Areas/Student/Views/Exams/Index.cshtml`               | Upcoming + completed lists. |
| `std-exam` (active) | `ExamInterface`            | `GET /Student/Exams/Take/{attemptId}`                        | `ExamsController.Take`               | `Areas/Student/Views/Exams/Take.cshtml`                | Full-bleed layout (no role shell). Strict auth: attempt must belong to current user and be in window. Form POSTs (all anti-forgery): `POST /Student/Exams/Start/{examId}` → creates attempt and redirects to `Take/{attemptId}`. `POST /Student/Exams/SaveAnswer` (autosave per question — debounced 2s) `{attemptId, questionId, response}`. `POST /Student/Exams/Flag` `{attemptId, questionId, flagged}`. `POST /Student/Exams/Submit/{attemptId}` → finalises, redirects to results. Server enforces server-side timer (do not trust client clock). |
| `std-results`     | `StudentResults`             | `GET /Student/Results`                                       | `ResultsController.Index`            | `Areas/Student/Views/Results/Index.cshtml`             | Latest result card + topic breakdown + all-results table. Read-only; `[Authorize]` + ownership check (`attempt.StudentId == currentUserId`). |
| `std-results` (one) | `StudentResults`           | `GET /Student/Results/{attemptId}`                           | `ResultsController.Details`          | `Areas/Student/Views/Results/Details.cshtml`           | Same data with deep link. |
| `std-analytics`   | `StudentAnalytics` (inline)  | `GET /Student/Analytics`                                     | `AnalyticsController.Index`          | `Areas/Student/Views/Analytics/Index.cshtml`           | Subject score trend bars + KPI cards. Read-only. |
| `std-leaderboard` | (placeholder in shell)       | `GET /Student/Leaderboard`                                   | `LeaderboardController.Index`        | `Areas/Student/Views/Leaderboard/Index.cshtml`         | Stub; React shell renders fallback dashboard for this view. |

### 4. Shared layout mapping

The React app does **not** share a `_Layout` between roles — every shell is its
own full-page component. That duplication should be collapsed in MVC:

| React element                                   | MVC equivalent                                               |
| ----------------------------------------------- | ------------------------------------------------------------ |
| Top-level theme wrapper (`dark` class on root)  | Root `Views/Shared/_Layout.cshtml` with theme cookie + `data-bs-theme` on `<body>`. |
| `LandingPage` full-bleed split layout           | `Views/Shared/_LayoutAnonymous.cshtml` (used by `Home`, `Account`). |
| Each role `Shell` (sidebar + topbar + main + mobile bottom nav) | Per-area `Areas/<Role>/Views/Shared/_RoleLayout.cshtml`. Each `_RoleLayout` `@RenderSection`s the page body and pulls common bits via partials. |
| Sidebar `NAV` array + brand block + user card   | `_Sidebar.cshtml` partial, populated via a `RoleNavViewComponent` that returns the nav model for the current role; active link computed from `ViewContext.RouteData`. |
| Top bar (search, dark toggle, bell, role badge) | `_TopBar.cshtml` partial + `NotificationsViewComponent` (badge count) + `UserMenuViewComponent`. Search box posts to a global `SearchController.Suggest` JSON endpoint. |
| Mobile bottom nav (`<nav class="lg:hidden ...">`) | `_MobileBottomNav.cshtml` partial, same nav model as sidebar, surfaced under `d-lg-none` Bootstrap classes. |
| `ExamInterface` header + sidebar (no shell)     | Distinct `_LayoutExam.cshtml`: minimal head, no role shell, includes server-driven countdown timer and a hidden anti-forgery form for the autosave endpoint. |

Reusable view components / partials to extract:

- `KpiCardViewComponent(label, value, sub, trend, iconKey, accentKey)`.
- `ChartCardViewComponent(title, dataUrl, type)` — chart bodies render
  client-side from a JSON endpoint; **Chart.js** replaces Recharts under the
  Bootstrap-only constraint.
- `DataTablePartial` (`_DataTable.cshtml`) for the search + filter + paginated
  table pattern that recurs in `InstituteManagement`, `InstituteUsers`,
  `QuestionBank`, `AuditLogs`, etc.
- `FilterBarPartial` (search input + selects).
- `PaginationPartial` (server-side pagination, query-string driven).
- `RoleBadgePartial`.

### 5. Forms, validation, anti-forgery, and POST endpoints

Every form below MUST include `@Html.AntiForgeryToken()` (or the implicit
`asp-antiforgery` from the tag helper) and the action MUST be decorated with
`[ValidateAntiForgeryToken]`. `[AutoValidateAntiforgeryToken]` is recommended
as a global filter so a missed attribute doesn't silently disable protection.
Validation lives in the view-model with DataAnnotations and is mirrored
client-side via `jquery-validation-unobtrusive`.

| Form / action                                  | Method + URL                                              | View model fields (proposed)                                                                 | Auth                  |
| ---------------------------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------------------------------- | --------------------- |
| Login                                          | `POST /Account/Login`                                     | `Username (Required, MaxLen)`, `Password (Required)`, `Role`, `RememberDevice (bool)`, `ReturnUrl` | Anon, rate-limited    |
| Logout                                         | `POST /Account/Logout`                                    | (empty)                                                                                      | User                  |
| Onboard institute                              | `POST /SuperAdmin/Institutes/Create`                      | `Name`, `Type`, `City`, `State`, `LicenseTier`, `AdminEmail` (`EmailAddress`, required)      | SuperAdmin            |
| Edit / suspend institute                       | `POST /SuperAdmin/Institutes/Edit/{id}` / `Suspend/{id}`  | id-bound, plus reason on suspend                                                             | SuperAdmin            |
| Approve / reject platform request              | `POST /SuperAdmin/Approvals/Approve/{id}` / `Reject/{id}` | `RequestId`, optional `Comment`                                                              | SuperAdmin            |
| Broadcast message                              | `POST /SuperAdmin/Broadcasts/Create`                      | `Subject`, `Body (HTML, sanitized)`, `Audience[]`, `ScheduledAt?`                            | SuperAdmin            |
| Invite user                                    | `POST /Institute/Users/Invite`                            | `Name`, `Email (EmailAddress)`, `Role`, `Department`                                         | InstituteAdmin        |
| Update user role / status                      | `POST /Institute/Users/Update/{id}`                       | `Role`, `Status`                                                                             | InstituteAdmin        |
| Approve / reject paper                         | `POST /Institute/Approvals/Approve/{id}` / `Reject/{id}`  | `PaperId`, `Comment` (required on reject)                                                    | InstituteAdmin        |
| Create / edit question                         | `POST /Teacher/Questions/Create` / `Edit/{id}`            | `Type`, `Subject`, `Topic`, `Class`, `Difficulty`, `Bloom`, `Marks`, `BodyHtml` (TinyMCE, sanitized via HtmlSanitizer), `Choices[]` (MCQ), `CorrectAnswer`, `RubricCriteria[]` (Descriptive), `TestCases[]` (Coding) | Teacher (own)         |
| Delete question                                | `POST /Teacher/Questions/Delete/{id}`                     | id-bound; ownership check                                                                    | Teacher (own)         |
| Excel import                                   | `POST /Institute/Questions/Import`                        | `IFormFile File` (`.xlsx` only, ≤ 10 MB, MIME + extension validated)                         | InstituteAdmin        |
| Save paper draft                               | `POST /Teacher/Papers/SaveDraft`                          | `PaperId?`, `Title`, `QuestionIds[]` (ordered), `BlueprintId?`                               | Teacher (own paper)   |
| Submit paper for approval                      | `POST /Teacher/Papers/Submit/{id}`                        | `PaperId`                                                                                    | Teacher (own paper)   |
| Generate from blueprint                        | `POST /Teacher/Papers/GenerateFromBlueprint/{id}`         | `TotalMarks`, `DurationMinutes`, `TypeMix{ }`, `DifficultyMix{ }`, `Subjects[]`              | Teacher (own paper)   |
| Save evaluation rubric score (autosave)        | `POST /Teacher/Evaluation/SaveScore`                      | `SubmissionId`, `AnswerIndex`, `RubricIndex`, `Points`, `Feedback?`                           | Evaluator (assigned)  |
| Submit evaluation                              | `POST /Teacher/Evaluation/Submit/{submissionId}`          | `SubmissionId`                                                                                | Evaluator (assigned)  |
| Coding IDE: run code                           | `POST /Teacher/CodingIDE/Run`                             | `Language`, `Source`, `TestCases[]`                                                          | Teacher               |
| Coding IDE: save question                      | `POST /Teacher/CodingIDE/Save`                            | full Coding question payload                                                                 | Teacher               |
| Start exam attempt                             | `POST /Student/Exams/Start/{examId}`                      | `ExamId`                                                                                     | Student (eligible)    |
| Save answer (autosave)                         | `POST /Student/Exams/SaveAnswer`                          | `AttemptId`, `QuestionId`, `Response`                                                        | Student (own attempt) |
| Flag question                                  | `POST /Student/Exams/Flag`                                | `AttemptId`, `QuestionId`, `Flagged (bool)`                                                  | Student (own attempt) |
| Submit attempt                                 | `POST /Student/Exams/Submit/{attemptId}`                  | `AttemptId`                                                                                  | Student (own attempt) |
| Theme / dark-mode toggle                       | `POST /Profile/Theme`                                     | `Theme ("light"|"dark")`                                                                     | User                  |
| Export endpoints (institutes, audit, reports)  | `GET /<Area>/<Resource>/Export?format=csv|xlsx&...`       | filter query string                                                                          | role-gated            |

### 6. Architectural rules (binding on every chunk)

Twelve non-negotiable rules. Every chunk PR description self-attests against
these.

1. **Layering.** Controllers ≤ ~15 lines per action: validate → authorize →
   map ViewModel → call service → return view. Every cross-action workflow
   lives in `Aimbys.Application`. No business logic in Razor views or JS.
2. **Workflow-driven, not switch-driven.** No controller contains a `switch`
   over a status enum. State changes go through
   `IWorkflowService.TransitionAsync(...)` against the workflow definition
   for that domain (Chunk 11).
3. **Tenancy.** `IInstituteScope.CurrentInstituteId` filters every query.
   Cross-tenant access ⇒ 404 (never 403 — avoids existence disclosure).
4. **Anti-forgery.** Global `AutoValidateAntiforgeryToken` is on (PR #5).
   No chunk disables it. POST actions still annotated `[ValidateAntiForgeryToken]`
   for clarity.
5. **Audit.** Every state change writes an `AuditLog` row via `IAuditWriter`.
   Verb format `EntityType.Action` (e.g. `Question.Approved`,
   `Result.Published`, `Institute.Suspended`).
6. **Sanitisation.** All TinyMCE / authored HTML passes `IHtmlSanitizer`
   (allow-list policy) before persistence.
7. **Immutability.** Approved questions, frozen blueprint versions, locked
   paper versions, published results are read-only. Edits create new
   versions, never mutate predecessors.
8. **Score preservation.** Four-stage pipeline:
   `DraftScore → EvaluatedScore → ModeratedScore → FinalPublishedScore`.
   Each stage preserves timestamp, actor, override reason. None overwrites
   another. Re-evaluation appeals create new versions of every downstream
   stage.
9. **File storage.** All uploads go through `IFileStorageService`
   (Chunk 9; the local-disk binding is `LocalFileStorageService` /
   `ILocalFileStorageService`). Uploads are MIME- and size-validated by
   the caller's allow-list, streamed through SHA-256, and written under
   `<ContentRoot>/uploads/<area>/{guid}{hash-prefix}.{ext}` &mdash; no
   user-supplied path component ever lands on disk. Downloads via the
   authorised `GET /files/{token}` endpoint, which writes a `File.Read`
   audit row and applies the area's permission policy
   (`Aimbys.Domain.Permissions.FileAreaPolicies`). Folder map is fixed:
   `/uploads/{questions|papers|answers|certificates|reports|exams|coding|temp}`.
10. **Permissions.** `[RequiresPermission("...")]` (Chunk 8) is the only
    sanctioned check on operational capabilities (`CanEvaluate`,
    `CanModerate`, etc.). No raw `User.IsInRole("Evaluator")` — those are
    not Identity roles.
11. **Identity roles.** Exactly four:
    `SuperAdmin`, `InstituteAdmin`, `Teacher`, `Student`. Anything else
    (Evaluator, Moderator, Proctor, Reviewer, …) is a permission flag on
    `TeacherProfile`, dynamically assigned by the Institute Admin.
12. **No business logic in JS.** UI is observer-only. Every rule (timer
    cutoff, scoring math, eligibility, transition gating) re-checked on the
    server. Client-side enforcement is convenience, not security.

### 7. Build roadmap — Chunks 8 through 34

Twenty-seven chunks ahead, grouped into ten phases. Every chunk is a single
PR titled exactly as shown.

> Phase A lays the architectural groundwork (no user-visible features) so
> Phases B onward stay small and focused.

#### Phase A — Architectural prerequisites

| # | PR title | What it adds |
| --- | --- | --- |
| 8 | `Refactor: 13 teacher-permission flags + canonical Identity roles` | Replaces the 6 placeholder flags on `TeacherProfile` with the canonical 13 (`CanCreateQuestions`, `CanGeneratePaper`, `CanManageBlueprints`, `CanEvaluate`, `CanModerate`, `CanPublishResults`, `CanScheduleExam`, `CanReviewCodingQuestions`, `CanManageQuestionBank`, `CanAssignEvaluators`, `CanManageAnalytics`, `CanApproveQuestions`, `CanProctor`). Shrinks `Roles.cs` to the canonical four. Adds `IPermissionGuard` + `[RequiresPermission(...)]` action filter. EF migration. |
| 9 | `Infra: ILocalFileStorageService + secure-download authorization` | Centralised local storage with MIME / size guards, sha256 content hash, audit-tracked `FileAsset` rows, token-based authorised downloads. Fixed folder map. |
| 10 | `Infra: domain events + in-app notifications + activity feed` | `IDomainEventDispatcher` (post-commit via `ISaveChangesInterceptor`), `Notification` entity (institute-scoped), `INotificationService`, `NotificationsViewComponent` for the topbar bell, activity-feed pages per area. |
| 11 | `Infra: workflow engine (states, transitions, queues, assignments)` | `WorkflowDefinition` / `WorkflowInstance` / `WorkflowState` / `ApprovalQueue` / `TaskAssignment` / `ReviewerAssignment` / `ModerationQueue`. Pre-registered definitions: `QuestionApproval`, `PaperApproval`, `EvaluationReview`, `ResultPublication`, `InstituteApproval`. Audit + domain event fired on every transition. |

#### Phase B — Anonymous & role shells

| # | PR title | What it adds |
| --- | --- | --- |
| 12 | `UI: PARAKH role-pick landing + first-view redirect on sign-in` | Recreates `LandingPage.tsx` (hero + stats + features + role tiles + login). `AccountController.Login` redirects to `/SuperAdmin`, `/Institute`, `/Teacher`, or `/Student` based on the user's Identity role. |
| 13 | `UI: SuperAdmin / Institute / Teacher / Student area layouts` | Four MVC Areas. One shared `_RoleLayout.cshtml` parameterised by accent (`#7c3aed` / `#1d4ed8` / `#0369a1` / `#15803d`). Partials: `_RoleSidebar`, `_RoleTopBar`, `_MobileBottomNav`. ViewComponents: `RoleNavViewComponent`, `NotificationsViewComponent`, `UserMenuViewComponent`. Replaces the placeholder `/Admin` from PR #5. |
| 14 | `UI: role dashboards + KpiCard / ChartCard / DataTable view components` | Chart.js (CDN, SRI). Reusable view-components (`KpiCardViewComponent`, `ChartCardViewComponent`) and partials (`_DataTable`, `_FilterBar`, `_PaginationPartial`, `_StatusBadge`, `_RoleBadge`). All four dashboards rendered with empty-state copy + sample chart endpoints. |

#### Phase C — Tenancy lifecycle

| # | PR title | What it adds |
| --- | --- | --- |
| 15 | `Feature: institute lifecycle (onboard / approve / reject / suspend / reactivate)` | Super Admin governance: thin `InstitutesController` over `IInstituteOnboardingService`. Every state transition runs through the `InstituteApproval` workflow (Chunk 11). Pending-approval KPI on Super Admin dashboard reads from the workflow's open queue. |
| 16 | `Feature: institute org tree (departments, years, subjects, class batches, streams, majors)` | New entities `Stream` and `Major` for cohort targeting; `Subject.StreamId?`/`MajorId?`, `ClassBatch.StreamId?`. Seven institute-admin CRUD controllers under `/Institute/Settings/*`. `IOrgTreeService` enforces invariants (one current academic year, no delete with active children). |
| 17 | `Feature: invite users + assign 13 teacher permission flags` | `UsersController` Index/Invite/Edit. Invite POST creates `IdentityUser` + matching profile + reset token, fires `UserInvited` event, hands off to `IUserInviteNotifier` (logging stub). Edit form lets the institute admin toggle the 13 permissions plus role-within-the-canonical-four / status / class batch. |

#### Phase D — Question authoring lifecycle

| # | PR title | What it adds |
| --- | --- | --- |
| 18 | `Feature: question authoring + revision history (TinyMCE, MCQ / Descriptive / Coding / Fill / TITA)` | `Question`, `QuestionVersion` (one per edit), `QuestionRevisionHistory` view, `QuestionOption`, `QuestionRubricCriterion`, `QuestionTestCase`, `QuestionAsset` (uses Chunk 9). Full status set `Draft/Submitted/UnderReview/Approved/Rejected/Retired/Archived`. Approved questions are immutable — Edit creates a new Draft version against the same `QuestionId`. TinyMCE wired with sanitised image upload. Excel import (`.xlsx`, ≤ 10 MB). |
| 19 | `Feature: question approval workflow (review queue + reviewer assignment)` | `QuestionReview`, `QuestionApproval`, `QuestionModeration` side-tables. `QuestionApproval` workflow drives `Submitted → UnderReview → Approved | Rejected`, with reviewer assignment via `ReviewerAssignment`. Required comment on rejection. `IQuestionLifecycleService` orchestrates every transition. |
| 20 | `Feature: question usage + difficulty audit (nightly aggregation)` | `QuestionUsageAnalytics`, `QuestionDifficultyAudit`. `QuestionAnalyticsAggregator` `BackgroundService` runs nightly (`PeriodicTimer`). Question-bank list rows render usage count + quality score (matches `QuestionBank.tsx`). |

#### Phase E — Blueprint + paper authoring

| # | PR title | What it adds |
| --- | --- | --- |
| 21 | `Feature: blueprint engine + cohort targeting (stream / major / dept / year / batch)` | `AssessmentDesign`, `Blueprint`, `BlueprintVersion`, `BlueprintSection`, `BlueprintConstraint` (chapter × competency × difficulty cell), `BlueprintCohort` (m:n with Stream/Major/Department/AcademicYear/ClassBatch), `Competency`, `Chapter`. Versioning rule: editing a Published blueprint creates a new `BlueprintVersion`; once a Paper references a version, that version is `IsLocked = true`. `IBlueprintValidator` re-checks every cell server-side. |
| 22 | `Feature: paper generation (manual + blueprint-driven) + version freeze + approval` | `Paper`, `PaperVersion` (immutable), `PaperSection`, `PaperQuestion` (ordered join), `PublishedSnapshot` (denormalised JSON taken at exam scheduling — analytics references this so historical reports survive future edits). Two authoring tabs (`PaperGeneration.tsx` reference): Question Bank picker + Auto Blueprint. `PaperApproval` workflow drives `Draft → SubmittedForApproval → Approved | Returned`. Once an `Exam` references a `PaperVersion`, that version locks. |

#### Phase F — Examination runtime

| # | PR title | What it adds |
| --- | --- | --- |
| 23 | `Feature: exam scheduling + student attempt with server-driven timer + autosave` | `Exam` (FK to `PaperVersionId`), `ExamAttempt`, `ExamAttemptAnswer`. Institute-side scheduling (gated by `CanScheduleExam`). Student lobby + `Take/{attemptId}` rendering distinct full-bleed `_LayoutExam.cshtml`. Server-driven timer; autosave / flag / submit POSTs. Auto-evaluation on submit for objective types only. `IExamRuntimeService` orchestrates everything. |
| 24 | `Feature: configurable exam security profile (fullscreen / tab-switch / heartbeat / event timeline)` | New entities: `ExamSecurityProfile` (per Paper, configured by paper setter), `ExamSession` (device fingerprint, IP, UA), `ExamEvent` (timeline row: `Started`, `FullscreenExit`, `TabBlur`, `Paste`, `KeyboardShortcut`, `ConnectionLost`, `ConnectionRestored`, `Heartbeat`, `AutoSubmitted`, `Submitted`). `ExamHeartbeatController` POST `/Student/Exams/Heartbeat`. Client JS hooks gated by the security profile flags. Suspicious-activity flag on `ExamAttempt` surfaces to evaluator + moderator. `IExamSecurityService` evaluates incoming events. |

#### Phase G — Evaluation, moderation, results

| # | PR title | What it adds |
| --- | --- | --- |
| 25 | `Feature: evaluation desk (rubric scoring, DraftScore → EvaluatedScore preserved)` | `ScoringScheme` (rubric template per `(PaperVersionId, QuestionId)`, frozen at paper-version freeze), `Evaluation`, `RubricScore`, `DraftScore` (autosave per criterion), `EvaluatedScore` (final on submit). `EvaluationReview` workflow governs `Pending → InProgress → Submitted → Returned`. `IEvaluationService` is the single place rubric values turn into scores. |
| 26 | `Feature: moderation desk + ModeratedScore + override audit` | `Moderation`, `ModeratedScore`, `ModerationSnapshot` (denormalised JSON of evaluator marks at moderation-open time). `ModerationController` Approve / RequireChanges / Override. Override does **not** mutate `EvaluatedScore` — it writes a new `ModeratedScore` row, both visible in the answer's score timeline. |
| 27 | `Feature: result publication + re-evaluation appeals` | `Result`, `FinalPublishedScore` (computed from `ModeratedScore` after publication), `ResultAppeal` (`Open / UnderReview / UpheldOriginal / Adjusted / Closed`). Publication gated by `ResultPublication` workflow: requires 100% of attempts moderated. Batch ranks computed at publish time. Appeal flow re-opens an `Evaluation`, re-runs Chunks 25–26 for that attempt, writes new versions of every downstream score (never overwriting). |

#### Phase H — Analytics

| # | PR title | What it adds |
| --- | --- | --- |
| 28 | `Feature: AnalyticsSnapshot + AggregatedAnalytics + CachedLeaderboard + nightly jobs` | `AnalyticsSnapshot`, `AggregatedAnalyticsTable` (per-subject mastery, per-evaluator efficiency), `CachedLeaderboardEntry`. Four `BackgroundService` aggregators run nightly (`InstituteAnalyticsAggregator`, `StudentPerformanceAggregator`, `EvaluatorEfficiencyAggregator`, `LeaderboardRecomputer`). All Chunk 14 dashboards now read from snapshots; raw EF queries reserved for the aggregators. |
| 29 | `Feature: GlobalAnalytics + InstituteAnalytics + TeacherReports + StudentAnalytics + Leaderboard` | Heatmap (chapter × competency, CSS-grid component fed from JSON), radar (question-type distribution per institute), trend lines (monthly, pass/fail, evaluator efficiency), batch-scoped leaderboard that never reveals other students' raw scores. |

#### Phase I — Special modes

| # | PR title | What it adds |
| --- | --- | --- |
| 30 | `Feature: multilingual content (Language, QuestionTranslation, PaperLanguageSet)` | Institute-scoped `Language`, `QuestionTranslation` (1:n from `Question`), `PaperLanguageSet`, `StudentProfile.PreferredLanguage`. Authoring screen gains a Translations tab; exam runtime auto-picks the student's preferred language with fallback. |
| 31 | `Feature: coding exam (Monaco editor, sandboxed runner, hidden + sample test cases)` | Monaco wired into `_LayoutExam`. `ICodeExecutionService` (Application abstraction) with `RunSampleAsync` / `RunFullAsync`. `ProcessIsolatedCodeExecutor` (Linux: `timeout` + `setrlimit`-style; Windows: JobObject). `Question.CodeStub`, `Question.Languages[]`, `QuestionTestCase.IsHidden`, `CodingSubmission`, `CodingTestCaseResult`. `CodingEvaluation` extends `Evaluation` for evaluator overrides (gated by `CanReviewCodingQuestions`). `IPlagiarismScorer` interface (logging stub now). |
| 32 | `Feature: advanced question types (case study + file upload + KaTeX equations)` | Case study (parent + sub-questions in one card), file-upload answers (uses Chunk 9), KaTeX renders `<span data-math="...">` in question body and rubric. |

#### Phase J — Governance & hardening

| # | PR title | What it adds |
| --- | --- | --- |
| 33 | `Feature: Super Admin governance (audit, system health, broadcasts)` | Audit log viewer (paginated, filterable, CSV export, read-only). System health (CPU / memory / req rate / DB ping / `IHostedService` registry). Broadcasts: scheduled HTML messages (sanitised) shown as a top banner across role shells during the active window. |
| 34 | `Hardening: CSP, rate limiting, exports, branding, QR/watermark, GitHub Actions CI` | CSP middleware (TinyMCE / Monaco / Chart.js / KaTeX CDNs allow-listed with SRI). Rate limiting on auth + exports + heartbeat. Output caching on landing + login + public broadcasts. `HSTS` / `Referrer-Policy` / `X-Content-Type-Options` / `X-Frame-Options`. Export endpoints (CSV / XLSX). Institute branding (logo + primary colour) flows into `_RoleLayout` per-tenant theme. QR verification on printed papers + result PDFs. Watermarking on prints + exports. GitHub Actions CI: `dotnet restore + build`. |

### 8. Definition of Done (per chunk PR)

A chunk is "done" only when every box below is ticked. Each PR description
self-attests by repeating the checklist with checkmarks.

- [ ] Routes registered, reachable from the sidebar / nav for the
      corresponding role(s).
- [ ] `[Authorize(Roles = "...")]` on every controller; `[RequiresPermission("...")]`
      on every action that needs an operational capability; ownership /
      tenancy enforced (cross-tenant ⇒ 404).
- [ ] All workflow transitions go through `IWorkflowService` — no `switch`
      over status enums in controllers.
- [ ] Views render against dedicated ViewModels — no EF entities leaked.
- [ ] All forms include `@Html.AntiForgeryToken()`; all POST actions have
      `[ValidateAntiForgeryToken]`. POST endpoints reject `GET`.
- [ ] Server-side validation via DataAnnotations; client validation via
      `jquery-validation-unobtrusive`. Every business rule is re-checked
      server-side regardless of what JS already validated.
- [ ] User-supplied HTML (TinyMCE bodies, feedback, rubric criteria, paper
      instructions) is sanitised on the server before persistence.
- [ ] All file uploads go through `ILocalFileStorageService`; downloads via
      authorised token endpoint with audit row.
- [ ] Bootstrap 5 responsive: layout verified at sm / md / lg / xl
      breakpoints; mobile bottom nav works for role pages; tables wrap or
      scroll, never overflow horizontally.
- [ ] Empty / loading / error states are visibly designed (never a blank
      page). 404 / 403 / 500 route through shared error views.
- [ ] Accessibility: form fields have `<label>`, interactive icons have
      `aria-label`, modals / dropdowns trap focus, colour contrast ≥ AA,
      keyboard navigation covers every interactive control.
- [ ] No secrets / connection strings / API keys committed; configuration
      from `IConfiguration` + user-secrets only.
- [ ] Audit row written for every state-changing action via `IAuditWriter`.
- [ ] Domain event fired (where applicable) so notifications / activity feed
      / aggregators downstream pick the change up.
- [ ] Immutability respected: approved questions, frozen blueprint versions,
      locked paper versions, published results never mutated; edits create
      new versions.
- [ ] `dotnet build` passes with **0 warnings, 0 errors**. Any tests added
      for the chunk pass under `dotnet test`.
- [ ] React reference cited: each PR description names the source `.tsx`
      file(s) it mirrors.
