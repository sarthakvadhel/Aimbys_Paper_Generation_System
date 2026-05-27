using System.Diagnostics;
using Aimbys.Application.Dashboard;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Dashboard;

/// <summary>
/// Default <see cref="IDashboardService"/> implementation. Reads directly
/// from <see cref="AppDbContext"/> with <c>AsNoTracking()</c> on every
/// query &mdash; dashboards are strictly read-only.
///
/// <para>
/// Strategy: compute every aggregate from the live tables on each
/// request. Bucketed time-series queries pull the raw rows in the
/// window (small) and group in memory to avoid EF translation
/// surprises. When the platform grows past a few thousand exam
/// attempts/day this layer is the natural place to swap in the
/// snapshot-aggregation tables already declared in
/// <c>AnalyticsSnapshot</c>; the controller contract does not change.
/// </para>
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private const int RecentRowLimit = 6;

    private readonly AppDbContext _db;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(AppDbContext db, ILogger<DashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // =====================================================================
    // SuperAdmin
    // =====================================================================

    public async Task<SuperAdminDashboardSnapshot> GetSuperAdminSnapshotAsync(
        CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStartUtc = nowUtc.Date.AddDays(-(int)nowUtc.DayOfWeek);
        var todayStartUtc = nowUtc.Date;
        var windowStartUtc = nowUtc.AddHours(-24);

        // ---- Top-line counts ------------------------------------------------
        var totalInstitutes = await _db.Institutes
            .AsNoTracking()
            .CountAsync(i => !i.IsDeleted, ct);

        var institutesAddedThisMonth = await _db.Institutes
            .AsNoTracking()
            .CountAsync(i => !i.IsDeleted && i.CreatedAtUtc >= monthStartUtc, ct);

        var registeredUsers = await _db.Users.AsNoTracking().CountAsync(ct);

        // No "user created at" on AspNetUsers by default, so use combined
        // teacher + student profile creation as a proxy.
        var usersAddedThisWeek =
            await _db.TeacherProfiles.AsNoTracking().CountAsync(t => t.CreatedAtUtc >= weekStartUtc, ct)
          + await _db.StudentProfiles.AsNoTracking().CountAsync(s => s.CreatedAtUtc >= weekStartUtc, ct);

        var papersGenerated = await _db.Papers.AsNoTracking().CountAsync(ct);

        var papersGeneratedToday = await _db.Papers
            .AsNoTracking()
            .CountAsync(p => p.CreatedAtUtc >= todayStartUtc, ct);

        var activeExams = await _db.Exams
            .AsNoTracking()
            .CountAsync(e => e.Status == ExamStatus.Live, ct);

        var candidatesOnline = await _db.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.Status == AttemptStatus.InProgress, ct);

        // ---- Platform activity: 24 hourly buckets ---------------------------
        var platformActivity = await BuildPlatformActivityFeedAsync(windowStartUtc, ct);

        // ---- Regional distribution: top-N states by institute count --------
        var regionalDistribution = await BuildRegionalFeedAsync(ct);

        // ---- License tier mix ----------------------------------------------
        var licenseTierMix = await BuildLicenseTierMixFeedAsync(ct);

        // ---- Pending approvals (real institute onboarding requests) --------
        var pendingApprovals = await _db.Institutes
            .AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status == InstituteStatus.PendingApproval)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(8)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Code,
                i.LicenseTier,
                i.CreatedAtUtc
            })
            .ToListAsync(ct);

        var pendingApprovalRows = pendingApprovals
            .Select(i => new PendingApprovalRow(
                DisplayId: i.Code,
                InstituteName: i.Name,
                Type: "New Institute",
                SubmittedAtUtc: i.CreatedAtUtc,
                Tier: i.LicenseTier.ToString()))
            .ToList();

        // ---- Service health (real signals, not placeholders) ---------------
        var serviceHealth = await BuildServiceHealthAsync(nowUtc, ct);

        return new SuperAdminDashboardSnapshot(
            TotalInstitutes: totalInstitutes,
            InstitutesAddedThisMonth: institutesAddedThisMonth,
            RegisteredUsers: registeredUsers,
            UsersAddedThisWeek: usersAddedThisWeek,
            PapersGenerated: papersGenerated,
            PapersGeneratedToday: papersGeneratedToday,
            ActiveExams: activeExams,
            CandidatesOnline: candidatesOnline,
            PlatformActivity: platformActivity,
            RegionalDistribution: regionalDistribution,
            LicenseTierMix: licenseTierMix,
            PendingApprovals: pendingApprovalRows,
            ServiceHealth: serviceHealth);
    }

    private async Task<ChartFeed> BuildPlatformActivityFeedAsync(
        DateTime windowStartUtc, CancellationToken ct)
    {
        // Pull raw timestamps in the window (small, indexed columns) and
        // bucket in memory so we don't depend on EF's translation of
        // SQL date functions.
        var examStarts = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => a.StartedAtUtc != null && a.StartedAtUtc >= windowStartUtc)
            .Select(a => a.StartedAtUtc!.Value)
            .ToListAsync(ct);

        var paperCreations = await _db.Papers
            .AsNoTracking()
            .Where(p => p.CreatedAtUtc >= windowStartUtc)
            .Select(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        // "Active logins" proxy: distinct actors per bucket from AuditLog.
        var auditEntries = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.OccurredAtUtc >= windowStartUtc && a.ActorUserId != null)
            .Select(a => new { a.OccurredAtUtc, a.ActorUserId })
            .ToListAsync(ct);

        var labels = new List<string>(24);
        var examSeries = new double[24];
        var paperSeries = new double[24];
        var loginSeries = new double[24];

        for (var h = 0; h < 24; h++)
        {
            var bucketStart = windowStartUtc.AddHours(h);
            var bucketEnd = bucketStart.AddHours(1);
            labels.Add(bucketStart.ToString("HH:00"));

            examSeries[h] = examStarts.Count(t => t >= bucketStart && t < bucketEnd);
            paperSeries[h] = paperCreations.Count(t => t >= bucketStart && t < bucketEnd);
            loginSeries[h] = auditEntries
                .Where(a => a.OccurredAtUtc >= bucketStart && a.OccurredAtUtc < bucketEnd)
                .Select(a => a.ActorUserId!)
                .Distinct()
                .Count();
        }

        return new ChartFeed(
            Labels: labels,
            Series: new[]
            {
                new ChartSeries("Active Users", loginSeries),
                new ChartSeries("Active Exams", examSeries),
                new ChartSeries("Papers Created", paperSeries)
            });
    }

    private async Task<ChartFeed> BuildRegionalFeedAsync(CancellationToken ct)
    {
        var byState = await _db.Institutes
            .AsNoTracking()
            .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.State))
            .GroupBy(i => i.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(7)
            .ToListAsync(ct);

        if (byState.Count == 0)
        {
            return new ChartFeed(
                Labels: new[] { "No data" },
                Series: new[] { new ChartSeries("Institutes", new double[] { 0 }) });
        }

        return new ChartFeed(
            Labels: byState.Select(x => x.State).ToList(),
            Series: new[]
            {
                new ChartSeries("Institutes", byState.Select(x => (double)x.Count).ToArray())
            });
    }

    private async Task<ChartFeed> BuildLicenseTierMixFeedAsync(CancellationToken ct)
    {
        var rows = await _db.Institutes
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .GroupBy(i => i.LicenseTier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Stable display order, even if a tier has zero institutes.
        var ordered = new[] { LicenseTier.Standard, LicenseTier.Premium, LicenseTier.Enterprise }
            .Select(t => new
            {
                Label = t.ToString(),
                Count = rows.FirstOrDefault(r => r.Tier == t)?.Count ?? 0
            })
            .ToList();

        return new ChartFeed(
            Labels: ordered.Select(x => x.Label).ToList(),
            Series: new[]
            {
                new ChartSeries("Licenses", ordered.Select(x => (double)x.Count).ToArray())
            });
    }

    private async Task<IReadOnlyList<ServiceHealthRow>> BuildServiceHealthAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var rows = new List<ServiceHealthRow>();

        // ---- Database probe -------------------------------------------------
        var dbStopwatch = Stopwatch.StartNew();
        var dbStatus = "operational";
        try
        {
            // Cheap probe; AsNoTracking is implicit on a count.
            await _db.Institutes.AsNoTracking().Take(1).CountAsync(ct);
        }
        catch (Exception ex)
        {
            dbStopwatch.Stop();
            _logger.LogWarning(ex, "Dashboard database probe failed.");
            rows.Add(new ServiceHealthRow("Database", "critical", "n/a", "timeout"));
            dbStatus = "critical";
        }
        if (dbStatus == "operational")
        {
            dbStopwatch.Stop();
            var ms = (int)dbStopwatch.ElapsedMilliseconds;
            var status = ms > 500 ? "degraded" : "operational";
            rows.Add(new ServiceHealthRow("Database", status, "—", $"{ms}ms"));
        }

        // ---- Identity store -------------------------------------------------
        try
        {
            var probeStart = Stopwatch.GetTimestamp();
            await _db.Users.AsNoTracking().Take(1).CountAsync(ct);
            var elapsedMs = (int)(Stopwatch.GetElapsedTime(probeStart).TotalMilliseconds);
            var status = elapsedMs > 500 ? "degraded" : "operational";
            rows.Add(new ServiceHealthRow("Identity Store", status, "—", $"{elapsedMs}ms"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity store probe failed.");
            rows.Add(new ServiceHealthRow("Identity Store", "critical", "n/a", "—"));
        }

        // ---- Background jobs (last 24h failure ratio) -----------------------
        var dayAgo = nowUtc.AddDays(-1);
        var jobsTotal = await _db.ScheduledJobs
            .AsNoTracking()
            .CountAsync(j => j.LastRunAtUtc != null && j.LastRunAtUtc >= dayAgo, ct);
        var jobsFailed = await _db.ScheduledJobs
            .AsNoTracking()
            .CountAsync(j => j.Status == ScheduledJobStatus.Failed && j.LastRunAtUtc >= dayAgo, ct);

        var jobsStatus = jobsFailed == 0
            ? "operational"
            : (jobsFailed > Math.Max(1, jobsTotal / 5) ? "critical" : "degraded");
        var jobsLatency = jobsTotal == 0 ? "idle" : $"{jobsFailed}/{jobsTotal} failed";
        rows.Add(new ServiceHealthRow("Background Jobs", jobsStatus, "—", jobsLatency));

        // ---- Notification bus (recent throughput) ---------------------------
        var hourAgo = nowUtc.AddHours(-1);
        var notifLastHour = await _db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.CreatedAtUtc >= hourAgo, ct);
        rows.Add(new ServiceHealthRow(
            "Notification Bus",
            "operational",
            "—",
            $"{notifLastHour}/h"));

        // ---- Workflow engine -----------------------------------------------
        var wfOpen = await _db.WorkflowInstances
            .AsNoTracking()
            .CountAsync(w => !w.IsCompleted, ct);
        rows.Add(new ServiceHealthRow(
            "Workflow Engine",
            "operational",
            "—",
            $"{wfOpen} open"));

        return rows;
    }

    // =====================================================================
    // Institute
    // =====================================================================

    public async Task<InstituteDashboardSnapshot> GetInstituteSnapshotAsync(
        Guid instituteId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStartUtc = nowUtc.Date.AddDays(-(int)nowUtc.DayOfWeek);
        var sevenDaysAgoUtc = nowUtc.Date.AddDays(-6);   // 7-day window incl. today

        var totalStudents = await _db.StudentProfiles
            .AsNoTracking()
            .CountAsync(s => s.InstituteId == instituteId && s.Status == ProfileStatus.Active, ct);

        var studentsAddedThisMonth = await _db.StudentProfiles
            .AsNoTracking()
            .CountAsync(s => s.InstituteId == instituteId && s.CreatedAtUtc >= monthStartUtc, ct);

        var papersGenerated = await _db.Papers
            .AsNoTracking()
            .CountAsync(p => p.InstituteId == instituteId, ct);

        var papersGeneratedThisMonth = await _db.Papers
            .AsNoTracking()
            .CountAsync(p => p.InstituteId == instituteId && p.CreatedAtUtc >= monthStartUtc, ct);

        var questionBankSize = await _db.Questions
            .AsNoTracking()
            .CountAsync(q => q.InstituteId == instituteId, ct);

        var questionsAddedThisMonth = await _db.Questions
            .AsNoTracking()
            .CountAsync(q => q.InstituteId == instituteId && q.CreatedAtUtc >= monthStartUtc, ct);

        var examsScheduledOpen = await _db.Exams
            .AsNoTracking()
            .CountAsync(e => e.InstituteId == instituteId
                          && (e.Status == ExamStatus.Scheduled || e.Status == ExamStatus.Published),
                        ct);

        var examsScheduledThisWeek = await _db.Exams
            .AsNoTracking()
            .CountAsync(e => e.InstituteId == instituteId
                          && e.ScheduledAtUtc >= weekStartUtc
                          && e.ScheduledAtUtc < weekStartUtc.AddDays(7),
                        ct);

        // ---- Weekly activity feed ------------------------------------------
        var weeklyActivity = await BuildInstituteWeeklyActivityAsync(
            instituteId, sevenDaysAgoUtc, ct);

        // ---- Subject performance feed --------------------------------------
        var subjectPerformance = await BuildInstituteSubjectPerformanceAsync(instituteId, ct);

        // ---- Top contributors (teachers ranked by paper output) ------------
        var topContributors = await BuildInstituteTopContributorsAsync(instituteId, ct);

        // ---- Recent alerts (real notifications) ----------------------------
        var alerts = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.InstituteId == instituteId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(6)
            .Select(n => new { n.Severity, n.Title, n.CreatedAtUtc })
            .ToListAsync(ct);

        var alertRows = alerts
            .Select(n => new RecentAlertRow(
                Severity: MapSeverity(n.Severity),
                Message: n.Title,
                OccurredAtUtc: n.CreatedAtUtc))
            .ToList();

        return new InstituteDashboardSnapshot(
            TotalStudents: totalStudents,
            StudentsAddedThisMonth: studentsAddedThisMonth,
            PapersGenerated: papersGenerated,
            PapersGeneratedThisMonth: papersGeneratedThisMonth,
            QuestionBankSize: questionBankSize,
            QuestionsAddedThisMonth: questionsAddedThisMonth,
            ExamsScheduledOpen: examsScheduledOpen,
            ExamsScheduledThisWeek: examsScheduledThisWeek,
            WeeklyActivity: weeklyActivity,
            SubjectPerformance: subjectPerformance,
            TopContributors: topContributors,
            RecentAlerts: alertRows);
    }

    private async Task<ChartFeed> BuildInstituteWeeklyActivityAsync(
        Guid instituteId, DateTime startDateUtc, CancellationToken ct)
    {
        // 7-day window starting at startDateUtc (today inclusive). Each
        // bucket is one calendar day in UTC.
        var windowEnd = startDateUtc.AddDays(7);

        var papers = await _db.Papers
            .AsNoTracking()
            .Where(p => p.InstituteId == instituteId
                     && p.CreatedAtUtc >= startDateUtc
                     && p.CreatedAtUtc < windowEnd)
            .Select(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        var exams = await _db.Exams
            .AsNoTracking()
            .Where(e => e.InstituteId == instituteId
                     && e.CreatedAtUtc >= startDateUtc
                     && e.CreatedAtUtc < windowEnd)
            .Select(e => e.CreatedAtUtc)
            .ToListAsync(ct);

        var labels = new List<string>(7);
        var paperSeries = new double[7];
        var examSeries = new double[7];

        for (var d = 0; d < 7; d++)
        {
            var dayStart = startDateUtc.AddDays(d);
            var dayEnd = dayStart.AddDays(1);
            labels.Add(dayStart.ToString("ddd"));

            paperSeries[d] = papers.Count(t => t >= dayStart && t < dayEnd);
            examSeries[d] = exams.Count(t => t >= dayStart && t < dayEnd);
        }

        return new ChartFeed(
            Labels: labels,
            Series: new[]
            {
                new ChartSeries("Papers", paperSeries),
                new ChartSeries("Exams", examSeries)
            });
    }

    private async Task<ChartFeed> BuildInstituteSubjectPerformanceAsync(
        Guid instituteId, CancellationToken ct)
    {
        // Average percentage and pass-rate per subject across published
        // results in the institute. Pass threshold = 40%.
        const double PassThresholdPercent = 40d;

        var rows = await (
            from r in _db.Results.AsNoTracking()
            where r.IsPublished
            join a in _db.ExamAttempts.AsNoTracking() on r.ExamAttemptId equals a.Id
            join e in _db.Exams.AsNoTracking() on a.ExamId equals e.Id
            where e.InstituteId == instituteId
            join pv in _db.PaperVersions.AsNoTracking() on e.PaperVersionId equals pv.Id
            join p in _db.Papers.AsNoTracking() on pv.PaperId equals p.Id
            join s in _db.Subjects.AsNoTracking() on p.SubjectId equals s.Id
            select new { Subject = s.Name, r.Percentage }
        ).ToListAsync(ct);

        if (rows.Count == 0)
        {
            return new ChartFeed(
                Labels: new[] { "No data" },
                Series: new[]
                {
                    new ChartSeries("Avg Score (%)", new double[] { 0 }),
                    new ChartSeries("Pass Rate (%)", new double[] { 0 })
                });
        }

        var grouped = rows
            .GroupBy(x => x.Subject)
            .Select(g => new
            {
                Subject = g.Key,
                Avg = Math.Round(g.Average(x => x.Percentage), 1),
                PassRate = Math.Round(
                    g.Count(x => x.Percentage >= PassThresholdPercent) * 100d / g.Count(), 1)
            })
            .OrderByDescending(x => x.Avg)
            .Take(8)
            .ToList();

        return new ChartFeed(
            Labels: grouped.Select(x => x.Subject).ToList(),
            Series: new[]
            {
                new ChartSeries("Avg Score (%)", grouped.Select(x => x.Avg).ToArray()),
                new ChartSeries("Pass Rate (%)", grouped.Select(x => x.PassRate).ToArray())
            });
    }

    private async Task<IReadOnlyList<TopContributorRow>> BuildInstituteTopContributorsAsync(
        Guid instituteId, CancellationToken ct)
    {
        // Authors ranked by paper count; approved-pct = (Approved+Published)/Total.
        var contributors = await (
            from p in _db.Papers.AsNoTracking()
            where p.InstituteId == instituteId
            join t in _db.TeacherProfiles.AsNoTracking() on p.AuthorTeacherProfileId equals t.Id
            select new { TeacherId = t.Id, t.DisplayName, p.Status, p.SubjectId }
        ).ToListAsync(ct);

        if (contributors.Count == 0)
        {
            return Array.Empty<TopContributorRow>();
        }

        // Resolve subject names for the top contributors only.
        var subjectIds = contributors.Select(c => c.SubjectId).Distinct().ToList();
        var subjects = await _db.Subjects
            .AsNoTracking()
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var ranked = contributors
            .GroupBy(c => new { c.TeacherId, c.DisplayName })
            .Select(g =>
            {
                var total = g.Count();
                var approved = g.Count(x => x.Status == PaperStatus.Approved
                                         || x.Status == PaperStatus.Published);
                var topSubjectId = g
                    .GroupBy(x => x.SubjectId)
                    .OrderByDescending(sg => sg.Count())
                    .First().Key;
                var pct = total == 0 ? 0 : (int)Math.Round(approved * 100d / total);
                return new TopContributorRow(
                    TeacherName: g.Key.DisplayName,
                    Subject: subjects.TryGetValue(topSubjectId, out var sn) ? sn : "—",
                    PapersAuthored: total,
                    ApprovedPercentage: pct);
            })
            .OrderByDescending(r => r.PapersAuthored)
            .Take(5)
            .ToList();

        return ranked;
    }

    // =====================================================================
    // Teacher
    // =====================================================================

    public async Task<TeacherDashboardSnapshot> GetTeacherSnapshotAsync(
        string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return EmptyTeacherSnapshot();
        }

        var profile = await _db.TeacherProfiles
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Id, t.InstituteId })
            .FirstOrDefaultAsync(ct);

        if (profile is null)
        {
            return EmptyTeacherSnapshot();
        }

        var teacherId = profile.Id;
        var nowUtc = DateTime.UtcNow;
        var yearStartUtc = new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var papersCreatedThisYear = await _db.Papers
            .AsNoTracking()
            .CountAsync(p => p.AuthorTeacherProfileId == teacherId && p.CreatedAtUtc >= yearStartUtc, ct);

        var papersPendingApproval = await _db.Papers
            .AsNoTracking()
            .CountAsync(p => p.AuthorTeacherProfileId == teacherId
                          && p.Status == PaperStatus.SubmittedForApproval, ct);

        var pendingEvaluationsCount = await _db.Evaluations
            .AsNoTracking()
            .CountAsync(e => e.EvaluatorTeacherProfileId == teacherId
                          && (e.Status == EvaluationStatus.Pending
                           || e.Status == EvaluationStatus.InProgress), ct);

        // Average percentage across published results for exams whose paper
        // this teacher authored.
        var resultPercentages = await (
            from r in _db.Results.AsNoTracking()
            where r.IsPublished
            join a in _db.ExamAttempts.AsNoTracking() on r.ExamAttemptId equals a.Id
            join e in _db.Exams.AsNoTracking() on a.ExamId equals e.Id
            join pv in _db.PaperVersions.AsNoTracking() on e.PaperVersionId equals pv.Id
            join p in _db.Papers.AsNoTracking() on pv.PaperId equals p.Id
            where p.AuthorTeacherProfileId == teacherId
            select r.Percentage
        ).ToListAsync(ct);

        var averageClassScorePercent = resultPercentages.Count == 0
            ? 0d
            : Math.Round(resultPercentages.Average(), 1);

        // ---- Class-average chart -------------------------------------------
        var classAvg = await BuildTeacherClassAverageFeedAsync(teacherId, ct);

        // ---- Recent papers --------------------------------------------------
        var recentPapers = await BuildTeacherRecentPapersAsync(teacherId, ct);

        // ---- Pending evaluations table -------------------------------------
        var pendingEvaluations = await BuildTeacherPendingEvaluationsAsync(teacherId, ct);

        return new TeacherDashboardSnapshot(
            PapersCreatedThisYear: papersCreatedThisYear,
            PapersPendingApproval: papersPendingApproval,
            PendingEvaluationsCount: pendingEvaluationsCount,
            AverageClassScorePercent: averageClassScorePercent,
            AverageClassScoreDelta: 0d,   // term-over-term delta requires academic year boundaries
            ClassAverageScores: classAvg,
            RecentPapers: recentPapers,
            PendingEvaluations: pendingEvaluations);
    }

    private async Task<ChartFeed> BuildTeacherClassAverageFeedAsync(
        Guid teacherProfileId, CancellationToken ct)
    {
        var rows = await (
            from r in _db.Results.AsNoTracking()
            where r.IsPublished
            join a in _db.ExamAttempts.AsNoTracking() on r.ExamAttemptId equals a.Id
            join e in _db.Exams.AsNoTracking() on a.ExamId equals e.Id
            join cb in _db.ClassBatches.AsNoTracking() on e.ClassBatchId equals cb.Id
            join pv in _db.PaperVersions.AsNoTracking() on e.PaperVersionId equals pv.Id
            join p in _db.Papers.AsNoTracking() on pv.PaperId equals p.Id
            where p.AuthorTeacherProfileId == teacherProfileId
            select new { ClassName = cb.Name, r.Percentage }
        ).ToListAsync(ct);

        if (rows.Count == 0)
        {
            return new ChartFeed(
                Labels: new[] { "No data" },
                Series: new[] { new ChartSeries("Avg Score (%)", new double[] { 0 }) });
        }

        var grouped = rows
            .GroupBy(x => x.ClassName)
            .Select(g => new
            {
                Class = g.Key,
                Avg = Math.Round(g.Average(x => x.Percentage), 1)
            })
            .OrderByDescending(x => x.Avg)
            .Take(8)
            .ToList();

        return new ChartFeed(
            Labels: grouped.Select(x => x.Class).ToList(),
            Series: new[]
            {
                new ChartSeries("Avg Score (%)", grouped.Select(x => x.Avg).ToArray())
            });
    }

    private async Task<IReadOnlyList<RecentPaperRow>> BuildTeacherRecentPapersAsync(
        Guid teacherProfileId, CancellationToken ct)
    {
        var rows = await (
            from p in _db.Papers.AsNoTracking()
            where p.AuthorTeacherProfileId == teacherProfileId
            orderby p.UpdatedAtUtc descending
            select new
            {
                p.Id,
                p.CurrentVersionId,
                p.Status,
                p.CreatedAtUtc
            }
        ).Take(RecentRowLimit).ToListAsync(ct);

        if (rows.Count == 0) return Array.Empty<RecentPaperRow>();

        var versionIds = rows
            .Where(r => r.CurrentVersionId != null)
            .Select(r => r.CurrentVersionId!.Value)
            .Distinct()
            .ToList();

        var versions = await _db.PaperVersions
            .AsNoTracking()
            .Where(v => versionIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Title, v.TotalMarks })
            .ToListAsync(ct);

        var qCounts = await _db.PaperQuestions
            .AsNoTracking()
            .Where(pq => versionIds.Contains(pq.VersionId))
            .GroupBy(pq => pq.VersionId)
            .Select(g => new { VersionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            var v = r.CurrentVersionId is null
                ? null
                : versions.FirstOrDefault(x => x.Id == r.CurrentVersionId);
            var qCount = r.CurrentVersionId is null
                ? 0
                : qCounts.FirstOrDefault(x => x.VersionId == r.CurrentVersionId)?.Count ?? 0;

            return new RecentPaperRow(
                DisplayId: $"P-{r.Id.ToString()[..8].ToUpperInvariant()}",
                Title: v?.Title ?? "(untitled paper)",
                CreatedAtUtc: r.CreatedAtUtc,
                Status: MapPaperStatus(r.Status),
                Questions: qCount,
                Marks: v?.TotalMarks ?? 0);
        }).ToList();
    }

    private async Task<IReadOnlyList<PendingEvaluationRow>> BuildTeacherPendingEvaluationsAsync(
        Guid teacherProfileId, CancellationToken ct)
    {
        var rows = await (
            from ev in _db.Evaluations.AsNoTracking()
            where ev.EvaluatorTeacherProfileId == teacherProfileId
               && (ev.Status == EvaluationStatus.Pending || ev.Status == EvaluationStatus.InProgress)
            join ans in _db.ExamAttemptAnswers.AsNoTracking() on ev.AttemptAnswerId equals ans.Id
            join a in _db.ExamAttempts.AsNoTracking() on ans.AttemptId equals a.Id
            join e in _db.Exams.AsNoTracking() on a.ExamId equals e.Id
            join sp in _db.StudentProfiles.AsNoTracking() on a.StudentProfileId equals sp.Id
            orderby ev.AssignedAtUtc descending
            select new
            {
                StudentName = sp.DisplayName,
                ExamTitle = e.Title,
                SubmittedAtUtc = a.SubmittedAtUtc ?? a.StartedAtUtc ?? ev.AssignedAtUtc,
                AttemptId = a.Id
            }
        ).Take(8).ToListAsync(ct);

        if (rows.Count == 0) return Array.Empty<PendingEvaluationRow>();

        // Per-attempt answer count, computed once.
        var attemptIds = rows.Select(r => r.AttemptId).Distinct().ToList();
        var counts = await _db.ExamAttemptAnswers
            .AsNoTracking()
            .Where(ans => attemptIds.Contains(ans.AttemptId))
            .GroupBy(ans => ans.AttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => new PendingEvaluationRow(
            StudentName: r.StudentName,
            ExamTitle: r.ExamTitle,
            SubmittedAtUtc: r.SubmittedAtUtc,
            AnswerType: "Descriptive",
            QuestionCount: counts.FirstOrDefault(c => c.AttemptId == r.AttemptId)?.Count ?? 0))
            .ToList();
    }

    private static TeacherDashboardSnapshot EmptyTeacherSnapshot() => new(
        PapersCreatedThisYear: 0,
        PapersPendingApproval: 0,
        PendingEvaluationsCount: 0,
        AverageClassScorePercent: 0d,
        AverageClassScoreDelta: 0d,
        ClassAverageScores: new ChartFeed(
            Labels: new[] { "No data" },
            Series: new[] { new ChartSeries("Avg Score (%)", new double[] { 0 }) }),
        RecentPapers: Array.Empty<RecentPaperRow>(),
        PendingEvaluations: Array.Empty<PendingEvaluationRow>());

    // =====================================================================
    // Student
    // =====================================================================

    public async Task<StudentDashboardSnapshot> GetStudentSnapshotAsync(
        string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return EmptyStudentSnapshot();
        }

        var profile = await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.DisplayName,
                s.RollNumber,
                s.ClassBatchId,
                s.InstituteId
            })
            .FirstOrDefaultAsync(ct);

        if (profile is null)
        {
            return EmptyStudentSnapshot();
        }

        var classBatch = await _db.ClassBatches
            .AsNoTracking()
            .Where(c => c.Id == profile.ClassBatchId)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(ct);

        var institute = await _db.Institutes
            .AsNoTracking()
            .Where(i => i.Id == profile.InstituteId)
            .Select(i => new { i.Code, i.Name })
            .FirstOrDefaultAsync(ct);

        var displayHeader = string.Join(" · ", new[]
        {
            classBatch?.Name ?? "—",
            string.IsNullOrEmpty(profile.RollNumber) ? null : $"Roll No. {profile.RollNumber}",
            institute?.Code ?? institute?.Name
        }.Where(s => !string.IsNullOrEmpty(s))!);

        var examsTaken = await _db.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.StudentUserId == userId
                          && (a.Status == AttemptStatus.Submitted
                           || a.Status == AttemptStatus.Evaluated
                           || a.Status == AttemptStatus.Published), ct);

        var publishedResults = await (
            from r in _db.Results.AsNoTracking()
            where r.IsPublished
            join a in _db.ExamAttempts.AsNoTracking() on r.ExamAttemptId equals a.Id
            where a.StudentUserId == userId
            join e in _db.Exams.AsNoTracking() on a.ExamId equals e.Id
            join pv in _db.PaperVersions.AsNoTracking() on e.PaperVersionId equals pv.Id
            join p in _db.Papers.AsNoTracking() on pv.PaperId equals p.Id
            join s in _db.Subjects.AsNoTracking() on p.SubjectId equals s.Id
            orderby r.PublishedAtUtc descending
            select new
            {
                ResultId = r.Id,
                ExamTitle = e.Title,
                ExamDateUtc = e.ScheduledAtUtc,
                r.TotalScore,
                r.MaxScore,
                r.Percentage,
                r.RankInBatch,
                r.PublishedAtUtc,
                Subject = s.Name
            }
        ).ToListAsync(ct);

        var averageScorePercent = publishedResults.Count == 0
            ? 0d
            : Math.Round(publishedResults.Average(x => x.Percentage), 1);

        var latestRank = publishedResults
            .Where(r => r.RankInBatch != null)
            .OrderByDescending(r => r.PublishedAtUtc)
            .Select(r => r.RankInBatch)
            .FirstOrDefault();

        var classSize = await _db.StudentProfiles
            .AsNoTracking()
            .CountAsync(s => s.ClassBatchId == profile.ClassBatchId
                          && s.Status == ProfileStatus.Active, ct);

        // ---- Next exam ------------------------------------------------------
        var nowUtc = DateTime.UtcNow;
        var nextExam = await (
            from e in _db.Exams.AsNoTracking()
            where e.ClassBatchId == profile.ClassBatchId
               && e.ScheduledAtUtc > nowUtc
               && (e.Status == ExamStatus.Scheduled
                || e.Status == ExamStatus.Published
                || e.Status == ExamStatus.Live)
            join pv in _db.PaperVersions.AsNoTracking() on e.PaperVersionId equals pv.Id
            orderby e.ScheduledAtUtc
            select new
            {
                e.Id,
                e.Title,
                e.ScheduledAtUtc,
                e.DurationMinutes,
                pv.TotalMarks,
                e.Status
            }
        ).FirstOrDefaultAsync(ct);

        NextExamInfo? nextExamInfo = null;
        if (nextExam is not null)
        {
            var startWindow = nextExam.ScheduledAtUtc.AddMinutes(-15);
            nextExamInfo = new NextExamInfo(
                ExamId: nextExam.Id,
                Title: nextExam.Title,
                ScheduledAtUtc: nextExam.ScheduledAtUtc,
                DurationMinutes: nextExam.DurationMinutes,
                TotalMarks: nextExam.TotalMarks,
                IsStartable: nowUtc >= startWindow && nextExam.Status != ExamStatus.Scheduled);
        }

        // ---- Recent results table -------------------------------------------
        var recentRows = publishedResults
            .Take(RecentRowLimit)
            .Select(r => new RecentResultRow(
                ExamTitle: r.ExamTitle,
                ExamDateUtc: r.ExamDateUtc,
                Score: r.TotalScore,
                MaxScore: r.MaxScore,
                Percentage: Math.Round(r.Percentage, 1)))
            .ToList();

        // ---- Subject progress chart -----------------------------------------
        ChartFeed subjectProgress;
        if (publishedResults.Count == 0)
        {
            subjectProgress = new ChartFeed(
                Labels: new[] { "No data" },
                Series: new[] { new ChartSeries("Average (%)", new double[] { 0 }) });
        }
        else
        {
            var grouped = publishedResults
                .GroupBy(r => r.Subject)
                .Select(g => new
                {
                    Subject = g.Key,
                    Avg = Math.Round(g.Average(r => r.Percentage), 1)
                })
                .OrderByDescending(x => x.Avg)
                .Take(8)
                .ToList();

            subjectProgress = new ChartFeed(
                Labels: grouped.Select(x => x.Subject).ToList(),
                Series: new[]
                {
                    new ChartSeries("Average (%)", grouped.Select(x => x.Avg).ToArray())
                });
        }

        return new StudentDashboardSnapshot(
            ExamsTaken: examsTaken,
            AverageScorePercent: averageScorePercent,
            ClassRank: latestRank,
            ClassSize: classSize == 0 ? null : classSize,
            NextExam: nextExamInfo,
            DisplayHeader: displayHeader,
            SubjectProgress: subjectProgress,
            RecentResults: recentRows);
    }

    private static StudentDashboardSnapshot EmptyStudentSnapshot() => new(
        ExamsTaken: 0,
        AverageScorePercent: 0d,
        ClassRank: null,
        ClassSize: null,
        NextExam: null,
        DisplayHeader: string.Empty,
        SubjectProgress: new ChartFeed(
            Labels: new[] { "No data" },
            Series: new[] { new ChartSeries("Average (%)", new double[] { 0 }) }),
        RecentResults: Array.Empty<RecentResultRow>());

    // =====================================================================
    // Helpers
    // =====================================================================

    private static string MapSeverity(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Information => "info",
        NotificationSeverity.Success => "success",
        NotificationSeverity.Warning => "warning",
        NotificationSeverity.Error => "critical",
        _ => "info"
    };

    private static string MapPaperStatus(PaperStatus status) => status switch
    {
        PaperStatus.Draft => "draft",
        PaperStatus.SubmittedForApproval => "pending",
        PaperStatus.Approved => "approved",
        PaperStatus.Returned => "rejected",
        PaperStatus.Published => "published",
        PaperStatus.Archived => "archived",
        _ => "draft"
    };
}
