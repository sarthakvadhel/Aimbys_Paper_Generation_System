# AIMBYS Enterprise Build Roadmap — Chunks 11–40 (Final Architecture Freeze)

## Status

- **Chunks 8–10 shipped and accepted** (PRs #9, #10, #11).
- This document is the **final canonical specification** for all implementation work from Chunk 11 onward.
- The React/Vite frontend (`src/`) remains the **visual and workflow source of truth**.
- NO chunk may deviate from the architectural rules below without explicit amendment.

---

## Locked Architecture Rules (binding on every chunk)

1. **Controllers ≤ 15 lines per action:** validate → authorize → map → call service → return.
2. **Workflow-driven:** `IWorkflowService.TransitionAsync(...)` for all state changes. NO `if(status == X)` in controllers.
3. **Tenancy:** `IInstituteScope.CurrentInstituteId` on every query. Cross-tenant → 404.
4. **Anti-forgery:** Global `AutoValidateAntiforgeryToken`. Never disabled.
5. **Audit:** `IAuditWriter` on every state change. Scoped visibility enforced.
6. **Sanitisation:** `IHtmlSanitizer` on all user-authored HTML before persistence.
7. **Immutability:** Approved questions, frozen blueprints, locked papers, published results — edits create new versions, never mutate.
8. **4-stage scoring:** `DraftScore → EvaluatedScore → ModeratedScore → FinalPublishedScore`. Each preserves actor + timestamp + reason. Never overwritten.
9. **File storage:** `ILocalFileStorageService` with MIME/size guards, sha256, audit-tracked downloads.
10. **Permissions:** `[RequiresPermission("...")]` only. No `User.IsInRole("Evaluator")`.
11. **Identity roles:** Exactly 4 (`SuperAdmin`, `InstituteAdmin`, `Teacher`, `Student`).
12. **No business logic in JS:** Observer-only; all rules re-checked server-side.
13. **Soft-delete:** `ISoftDelete` on destructive operations; hard-delete = SuperAdmin only.
14. **Scheduling:** Time-based logic via `ISchedulingService`, never hardcoded in controllers.
15. **Document rendering:** All PDF/paper/certificate generation via `IDocumentRenderService`.
16. **Bulk operations:** `IBulkOperationService`; controllers never implement loops.
17. **React fidelity:** Every chunk cites its source `.tsx` files and reproduces the same UX.
18. **Escalation:** Workflow deadlines + SLA tracking via `WorkflowEscalationRule`.
19. **Exposure governance:** `QuestionExposureLog` tracks usage; leakage indicators surface.
20. **Leaderboard privacy:** Visibility policy per institute; never expose raw marks publicly.

---

## Phase A — Enterprise Infrastructure

### Chunk 11 — Workflow Engine

**PR:** `Infra: workflow engine (states, transitions, queues, escalation, SLA)`

**Entities:** `WorkflowDefinition`, `WorkflowInstance`, `WorkflowState`, `WorkflowTransition`, `ApprovalQueue`, `TaskAssignment`, `ReviewerAssignment`, `ModerationQueue`, `WorkflowEscalationRule`, `WorkflowDeadline`, `WorkflowReminder`.

**Services:** `IWorkflowService` (Start/Transition/GetQueue/Assign), `IWorkflowEscalationService` (CheckDeadlines/SendReminder/Escalate).

**Pre-registered definitions:** InstituteApproval, QuestionApproval, PaperApproval, EvaluationReview, ModerationReview, ResultPublication, AppealReview, RecheckRequest.

**Acceptance:** All transitions via workflow engine; no `if(status==X)` anywhere; escalation job fires reminders; audit+event on every transition; `dotnet build` 0 warnings 0 errors.

---

### Chunk 12 — Soft-delete + Scheduling + Document Rendering + Bulk Ops + Config + Retention

**PR:** `Infra: soft-delete + scheduling + document rendering + bulk operations + central config + retention`

**Scope:** `ISoftDelete`/`IRestoreable` + global query filter. `ISchedulingService` (persistent/recurring/delayed jobs, Hangfire-compatible). `IDocumentRenderService` (PDF/papers/results/certificates/transcripts/exports). `IBulkOperationService` (import/assign/schedule/publish/activate/notify). `PlatformSetting`/`InstituteSetting`/`FeatureToggle` + `IConfigurationService`. `RetentionPolicy`/`ArchivePolicy` + enforcement job. Subscription lifecycle: `Trial/Active/Suspended/Expired/GracePeriod/RenewalPending` + enforcement middleware.

**Acceptance:** Soft-delete filter active; scheduling executes test jobs; rendering returns HTML V1; bulk import processes 50 rows; config cached with TTL; subscription middleware blocks expired; retention respects legal-hold; `dotnet build` 0/0.

---

### Chunk 13 — Notification Hardening + Audit Visibility Governance

**PR:** `Infra: notification templates + preferences + channels + scoped audit visibility`

**Entities:** `NotificationTemplate`, `NotificationTemplateTranslation`, `NotificationPreference`, `NotificationChannelConfig`, `AuditVisibilityRule`.

**Services:** `INotificationTemplateService` (render with interpolation + multilingual fallback), `INotificationPreferenceService`, `IAuditVisibilityService` (filter/mask by role/compliance).

**Acceptance:** Templates render with placeholders; multilingual fallback works; preferences gate delivery; audit visibility masks sensitive fields; `dotnet build` 0/0.

---

## Phase B — Anonymous & Role Shells

### Chunk 14 — PARAKH Role-Pick Landing + Post-Login Redirect

**PR:** `UI: PARAKH role-pick landing + first-view redirect on sign-in`

**React ref:** `src/app/components/aimbys/LandingPage.tsx`

**Scope:** Split-screen landing (hero+stats+features left, role-tiles+login right). Two-step login (pick role → credentials). Post-login redirect by Identity role. `RoleHomeRedirector` helper.

**Acceptance:** Anon `/` shows landing; role-pick works; login redirects to correct area; authenticated `/` auto-redirects; anti-forgery present; responsive sm/md/lg/xl; `dotnet build` 0/0.

---

### Chunk 15 — Four Role-Area Layouts

**PR:** `UI: SuperAdmin / Institute / Teacher / Student area layouts`

**React refs:** `SuperAdminShell.tsx`, `InstituteShell.tsx`, `TeacherShell.tsx`, `StudentShell.tsx`

**Scope:** Four MVC Areas with `[Authorize(Roles)]`. Shared `_RoleLayout.cshtml` (parameterised accent). `_RoleSidebar`, `_RoleTopBar` (with Notifications bell), `_MobileBottomNav`. `RoleNavViewComponent`, `UserMenuViewComponent`. Removes legacy `/Admin` area.

**Acceptance:** Four URLs render shells with correct accents/nav; sidebar collapses below lg; bell badge renders; anonymous → 302; `dotnet build` 0/0.

---

### Chunk 16 — Role Dashboards + Reusable View Components

**PR:** `UI: role dashboards + KpiCard / ChartCard / DataTable view components`

**React refs:** `SuperAdminDashboard.tsx`, `InstituteDashboard.tsx`, `TeacherDashboard.tsx`, `StudentDashboard.tsx`

**Scope:** Chart.js (CDN, SRI). `KpiCardViewComponent`, `ChartCardViewComponent`. Partials: `_DataTable`, `_FilterBar`, `_PaginationPartial`, `_StatusBadge`, `_RoleBadge`. Four dashboards with empty-state + sample chart endpoints.

**Acceptance:** All four dashboards render KPIs+charts+tables; view components reusable from any view; responsive; Chart.js no JS errors; `dotnet build` 0/0.

---

## Phase C — Tenancy Lifecycle

### Chunk 17 — Institute Lifecycle + Subscription

**PR:** `Feature: institute lifecycle (onboard/approve/reject/suspend/reactivate) + subscription`

**React ref:** `src/app/components/aimbys/superadmin/InstituteManagement.tsx`

**Scope:** `IInstituteOnboardingService`. All transitions via `InstituteApproval` workflow. Subscription state gates access. Pending-approval KPI on dashboard. Soft-delete on decommission. Status/type/search filters. CSV export via `IDocumentRenderService`.

**Acceptance:** Full lifecycle via workflow; subscription enforcement blocks expired; audit+event on transitions; export works; `dotnet build` 0/0.

---

### Chunk 18 — Institute Org Tree + Streams + Majors + Chapters

**PR:** `Feature: institute org tree + streams + majors + chapters + cohort targeting foundation`

**Scope:** New entities: `Stream`, `Major`, `Chapter`. Subject gains `StreamId?/MajorId?`. ClassBatch gains `StreamId?`. Seven CRUD controllers under `/Institute/Settings/*`. `IOrgTreeService` enforces invariants. Bulk class-batch creation. All implement `ISoftDelete`.

**Acceptance:** Full org tree buildable; invariants enforced; chapter reordering works; cross-tenant → 404; soft-delete works; `dotnet build` 0/0.

---

### Chunk 19 — Invite Users + Assign Permissions + Bulk Import

**PR:** `Feature: invite users + assign 13 teacher permissions + bulk student import`

**React ref:** `src/app/components/aimbys/institute/InstituteUsers.tsx`

**Scope:** `IUserManagementService` (Invite/Update/Suspend/Reactivate). 13 permission checkboxes for teachers. `IBulkOperationService.ImportStudentsAsync` for CSV/Excel. Role badges. KPI strip.

**Acceptance:** Invite creates user+profile; permissions save correctly; bulk 50-row CSV works with error reporting; suspend fires notification; cross-institute → 404; `dotnet build` 0/0.

---

## Phase D — Question Authoring Lifecycle

### Chunk 20 — Question Authoring + Versioning

**PR:** `Feature: question authoring + versioning (TinyMCE, all question types)`

**React refs:** `institute/QuestionBank.tsx`, `teacher/TeacherQuestionBank.tsx`

**Scope:** `Question`, `QuestionVersion` (immutable after approval), `QuestionOption`, `QuestionRubricCriterion`, `QuestionTestCase`, `QuestionAsset`, `QuestionExposureLog`. All question types (MCQ/MultiSelect/TrueFalse/FillBlanks/TITA/Descriptive/Coding/FileUpload/CaseStudy/Equation). TinyMCE with image upload. Excel import. Revision history view.

**Acceptance:** All types creatable; approved → immutable (edit creates new version); revision history shows lineage; TinyMCE images sanitised; Excel import works; cross-teacher → 404; `dotnet build` 0/0.

---

### Chunk 21 — Question Approval Workflow

**PR:** `Feature: question approval workflow (review queue + reviewer assignment + moderation)`

**Scope:** `QuestionReview`, `QuestionApproval`, `QuestionModeration`. `IQuestionLifecycleService` (Submit/AssignReviewer/Approve/Reject/Retire). `QuestionApproval` workflow: `Draft→Submitted→UnderReview→Approved|Rejected`. Escalation at 72h/120h. Auto-assign by subject+load.

**Acceptance:** Submit via workflow; reviewer assigned; approve locks version; reject requires comment and returns to Draft; escalation fires; all transitions audited+notified; `dotnet build` 0/0.

---

### Chunk 22 — Question Usage Analytics + Difficulty Audit + Exposure Governance

**PR:** `Feature: question usage analytics + difficulty audit + exposure governance`

**Scope:** `QuestionUsageAnalytics`, `QuestionDifficultyAudit`, `QuestionExposureLog`. Nightly aggregator computes P-value, discrimination index, mean time. Difficulty drift detection. Exposure risk indicators (>5 papers = high risk).

**Acceptance:** Aggregator computes metrics; drift flagged; exposure logged on paper publish; high-exposure indicator shown; analytics page renders; `dotnet build` 0/0.

---

## Phase E — Blueprint + Paper Authoring

### Chunk 23 — Blueprint Engine + Cohort Targeting

**PR:** `Feature: blueprint engine + cohort targeting + competency matrix + versioning`

**React ref:** `src/app/components/aimbys/teacher/PaperGeneration.tsx` (Auto Blueprint tab)

**Scope:** `Competency`, `AssessmentDesign`, `Blueprint`, `BlueprintVersion` (immutable once referenced), `BlueprintSection`, `BlueprintConstraint` (chapter×competency×difficulty), `BlueprintCohort` (m:n targeting). `IBlueprintAuthoringService`, `IBlueprintValidator`. Matrix editor UI. Cohort selectors.

**Acceptance:** Blueprint with sections+matrix creatable; validator rejects invalid sums; publish works; locked when paper references; cohort saves correctly; competency tree CRUD; cross-teacher → 404; `dotnet build` 0/0.

---

### Chunk 24 — Paper Generation + Version Freeze + Approval

**PR:** `Feature: paper generation (manual + blueprint) + version freeze + approval workflow`

**React ref:** `src/app/components/aimbys/teacher/PaperGeneration.tsx`

**Scope:** `Paper`, `PaperVersion` (immutable), `PaperSection`, `PaperQuestion`, `PublishedSnapshot`. Two-pane editor (bank picker + auto blueprint). `IPaperAssemblyService`, `IPaperValidationService`. `PaperApproval` workflow. Print preview via `IDocumentRenderService`. Version locked on exam reference.

**Acceptance:** Manual paper assembly works; blueprint generation auto-selects questions; submit/approve/return via workflow; paper version locks on exam scheduling; print preview renders; no duplicate questions; `dotnet build` 0/0.

---

## Phase F — Examination Runtime

### Chunk 25 — Exam Scheduling + Student Attempt Runtime

**PR:** `Feature: exam scheduling + student attempt (server-driven timer, autosave, auto-evaluation)`

**React ref:** `src/app/components/aimbys/student/ExamInterface.tsx`

**Scope:** `Exam`, `ExamAttempt`, `ExamAttemptAnswer`. `IExamSchedulingService`, `IExamRuntimeService`. `_LayoutExam.cshtml` (fullscreen). Server-driven timer. Autosave/flag/submit. Auto-evaluation for objective types. Auto-submit expired job. Exam status lifecycle: `Scheduled→Live→Completed`.

**Acceptance:** Schedule from approved paper; paper version+snapshot locked; student starts/takes/submits; timer enforced server-side; auto-eval for MCQ; expired auto-submitted; ownership enforced; `dotnet build` 0/0.

---

### Chunk 26 — Exam Security Profile + Event Timeline

**PR:** `Feature: exam security profile (fullscreen/tab-switch/heartbeat/event timeline)`

**Scope:** `ExamSecurityProfile`, `ExamSession`, `ExamEvent`, `ExamEventType` enum. `IExamSecurityService`. Client JS: visibility/fullscreen/paste/keyboard listeners + heartbeat. Suspicion threshold evaluation. Event timeline for evaluators.

**Acceptance:** Security profile configurable; tab-switch/fullscreen events recorded; heartbeat works; missed heartbeats trigger auto-submit; `IsSuspicious` flag set; timeline viewable; `dotnet build` 0/0.

---

## Phase G — Evaluation, Moderation, Results

### Chunk 27 — Evaluation Desk (DraftScore → EvaluatedScore)

**PR:** `Feature: evaluation desk (rubric scoring, DraftScore → EvaluatedScore preserved)`

**React ref:** `src/app/components/aimbys/teacher/EvaluationDesk.tsx`

**Scope:** `ScoringScheme` (frozen from paper version), `Evaluation`, `RubricScore`, `DraftScore` (autosave scratchpad), `EvaluatedScore` (immutable on submit). `IEvaluationAssignmentService`, `IEvaluationService`. `EvaluationReview` workflow. Split-view UI.

**Acceptance:** Auto-assignment after exam submit; evaluator inbox works; rubric autosaves; submit writes immutable EvaluatedScore; cannot submit until all criteria scored; cross-evaluator → 404; scoring scheme frozen; `dotnet build` 0/0.

---

### Chunk 28 — Moderation Desk + ModeratedScore + Override

**PR:** `Feature: moderation desk + ModeratedScore + override workflow`

**Scope:** `Moderation`, `ModeratedScore`, `ModerationSnapshot`. `IModerationService` (Enqueue/Approve/RequireChanges/Override). `ModerationReview` workflow. Override never mutates EvaluatedScore.

**Acceptance:** Auto-enqueue after evaluation submit; moderator inbox; approve=pass-through; RequireChanges returns to evaluator; override writes new ModeratedScore + reason (audited); snapshot preserves evaluator's original marks; `dotnet build` 0/0.

---

### Chunk 29 — Result Publication + Archives + Appeals

**PR:** `Feature: result publication + FinalPublishedScore + immutable archives + appeals`

**Scope:** `Result`, `FinalPublishedScore`, `ResultArchive`, `ResultAppeal`. `IResultPublicationService`, `IAppealService`. `ResultPublication` + `AppealReview` workflows. Immutable archives (PDF+JSON+analytics via `IDocumentRenderService`). Batch ranks. Leaderboard privacy. Appeals re-open evaluation chain without overwriting.

**Acceptance:** Cannot publish until 100% moderated; FinalPublishedScore computed; archives generated (immutable); ranks calculated; students see results only after publish; appeals trigger re-evaluation; adjusted appeal creates new score versions; leaderboard respects policy; `dotnet build` 0/0.

---

## Phase H — Analytics

### Chunk 30 — Analytics Snapshots + Aggregation + Leaderboard

**PR:** `Feature: AnalyticsSnapshot + aggregated tables + cached leaderboard + nightly jobs`

**Scope:** `AnalyticsSnapshot`, `AggregatedAnalyticsTable`, `CachedLeaderboardEntry`. Four nightly aggregators via `ISchedulingService`. Dashboards re-wired to read from snapshots.

**Acceptance:** Nightly jobs populate snapshots; dashboards read from snapshots (not live queries); leaderboard cached after publication; stale cleanup respects retention; `dotnet build` 0/0.

---

### Chunk 31 — Per-Role Analytics Screens

**PR:** `Feature: GlobalAnalytics + InstituteAnalytics + TeacherReports + StudentAnalytics + Leaderboard`

**React refs:** `GlobalAnalytics.tsx`, `InstituteAnalytics.tsx`, `TeacherDashboard.tsx`, `StudentDashboard.tsx`

**Scope:** Heatmap (chapter×competency CSS-grid), radar (question-type distribution), trend lines, leaderboard. Chart.js endpoints from snapshots. Export CSV. Leaderboard visibility policy.

**Acceptance:** All charts render with snapshot data; heatmap colours by mastery; leaderboard respects policy; export works; graceful empty state; `dotnet build` 0/0.

---

## Phase I — Special Modes

### Chunk 32 — Multilingual Content

**PR:** `Feature: multilingual question + paper authoring + exam-runtime language selection`

**Scope:** `Language`, `QuestionTranslation`, `PaperLanguageSet`, `NotificationTemplateTranslation`. `IMultilingualService`. Student `PreferredLanguageId`. Authoring Translations tab. Exam-runtime language selector with fallback. `MultilingualEnabled` feature toggle.

**Acceptance:** Institute creates languages; teacher authors translations; student sees preferred language in exam; fallback to default; feature toggle gates; print supports `?lang=`; `dotnet build` 0/0.

---

### Chunk 33 — Coding Exam (Monaco + Sandboxed Execution)

**PR:** `Feature: coding exam (Monaco editor, ICodeExecutionService, sandboxed execution)`

**React ref:** `TeacherShell.tsx` inline `CodingIDEPage`

**Scope:** `CodingSubmission`, `CodingTestCaseResult`, `CodeExecutionQueue`. `ICodeExecutionService` (RunSample/RunFull). `ProcessIsolatedCodeExecutor` (sandboxed, resource-limited, container-ready). `CodeExecutionQueueProcessor`. `IPlagiarismScorer` (stub). Monaco Editor (CDN, SRI).

**Acceptance:** Monaco loads; Run executes sample test cases; Submit runs all (hidden+visible); execution outside web process; timeout/memory enforced; evaluator override (`CanReviewCodingQuestions`); plagiarism interface called; `dotnet build` 0/0.

---

### Chunk 34 — Advanced Question Types (Case Study + File Upload + KaTeX)

**PR:** `Feature: advanced question types (case study + file upload + KaTeX equations)`

**Scope:** `Question.ParentQuestionId?` (case study hierarchy). File-upload answers via `ILocalFileStorageService`. KaTeX (CDN, SRI) for equation rendering. TinyMCE "Insert Equation" plugin.

**Acceptance:** Case study renders context + sub-questions; file upload stores+downloads correctly; KaTeX equations render in authoring+exam; `dotnet build` 0/0.

---

## Phase J — Governance & Hardening

### Chunk 35 — Super Admin Governance (Audit + Health + Broadcasts)

**PR:** `Feature: Super Admin governance (scoped audit, system health, broadcasts)`

**React refs:** `AuditLogs.tsx`, `SystemHealth.tsx`

**Scope:** Audit viewer (scoped visibility, paginated, filterable, CSV export). System health (CPU/memory/req metrics via `IRequestMetricsCollector`). `Broadcast` entity + top banner rendering.

**Acceptance:** Audit respects visibility rules; export works; system health charts render; broadcast appears as banner during scheduled window; `dotnet build` 0/0.

---

### Chunk 36 — Subscription Management + License Enforcement

**PR:** `Feature: subscription management + license enforcement + feature toggles UI`

**Scope:** SuperAdmin subscription CRUD. Tier-based feature gating (Standard/Premium/Enterprise). InstituteAdmin feature-toggles page. `ISubscriptionManagementService`. Daily expiration check job.

**Acceptance:** Tier change persists; expired blocked; grace period works; feature toggles respect tier; `dotnet build` 0/0.

---

### Chunk 37 — Bulk Operations UI

**PR:** `Feature: bulk operations UI (CSV import, batch assign, schedule, publish)`

**Scope:** Bootstrap-styled pages consuming `IBulkOperationService`. CSV/Excel upload with per-row error table. Template download. Progress feedback.

**Acceptance:** 100-row student import works; template downloadable; bulk exam scheduling creates multiple exams; controllers contain zero loop logic; `dotnet build` 0/0.

---

### Chunk 38 — CSP + Rate Limiting + Secure Headers

**PR:** `Hardening: CSP + rate limiting + output caching + HSTS + secure headers`

**Scope:** CSP (strict, CDN allowlisted with SRI). Rate limiting (login 5/min, exports 10/min, bulk 2/min). Output caching (landing, login). Secure headers (HSTS, Referrer-Policy, X-Content-Type-Options, X-Frame-Options, Permissions-Policy).

**Acceptance:** CSP blocks inline script; rate limit returns 429; cached responses served; all headers present; `dotnet build` 0/0.

---

### Chunk 39 — Exports + QR + Watermarking + Branding

**PR:** `Hardening: exports (CSV/XLSX) + QR verification + watermarking + branding`

**Scope:** Export endpoints (role-gated, audited). QR on papers (`/verify/paper/{token}`). Watermarking on PDFs. `PrintLog` entity. Institute branding (logo+colour in layout). `BrandingViewComponent`.

**Acceptance:** Exports download correctly; QR validates paper integrity; watermark visible; branding applies; PrintLog written; `dotnet build` 0/0.

---

### Chunk 40 — GitHub Actions CI + Accessibility + Responsive Final Pass

**PR:** `Hardening: GitHub Actions CI + accessibility + responsive final pass`

**Scope:** `.github/workflows/build.yml` (restore+build+test). Accessibility audit (labels, ARIA, contrast, focus, keyboard). Responsive verification (375/768/1024/1440px). Touch-friendly exam (44px targets). Dark-mode end-to-end. CSS cleanup.

**Acceptance:** CI runs on PR; all accessibility checks pass; no horizontal overflow; touch targets meet 44px; dark mode works; CI badge in README; `dotnet build` 0/0.

---

## Summary Table

| Phase | Chunks | Focus |
| --- | --- | --- |
| A — Infrastructure | 11, 12, 13 | Workflow, soft-delete/scheduling/rendering/bulk/config/retention, notifications+audit |
| B — Role Shells | 14, 15, 16 | Landing, four area layouts, dashboards+components |
| C — Tenancy | 17, 18, 19 | Institute lifecycle, org tree, user management |
| D — Questions | 20, 21, 22 | Authoring+versioning, approval workflow, usage analytics |
| E — Blueprint+Paper | 23, 24 | Blueprint+cohort, paper generation+approval |
| F — Examination | 25, 26 | Runtime+autosave, security profile+timeline |
| G — Eval/Mod/Results | 27, 28, 29 | Evaluation, moderation, publication+archives+appeals |
| H — Analytics | 30, 31 | Snapshot aggregation, per-role screens |
| I — Special Modes | 32, 33, 34 | Multilingual, coding exam, advanced types |
| J — Governance | 35, 36, 37, 38, 39, 40 | Audit/health/broadcasts, subscriptions, bulk ops, hardening |
