namespace Aimbys.Application.Dashboard;

// =============================================================================
// Shared chart primitives
// =============================================================================

/// <summary>
/// A single labelled numeric series used by the <c>ChartCard</c> JSON
/// endpoints. The view component renders the result as Chart.js JSON
/// directly.
/// </summary>
public sealed record ChartSeries(string Label, IReadOnlyList<double> Data);

/// <summary>
/// A pair of axis labels + one or more named series. Used by every
/// chart endpoint so the controller can serialise the snapshot
/// without knowing the chart type.
/// </summary>
public sealed record ChartFeed(IReadOnlyList<string> Labels, IReadOnlyList<ChartSeries> Series);

// =============================================================================
// SuperAdmin
// =============================================================================

public sealed record SuperAdminDashboardSnapshot(
    int TotalInstitutes,
    int InstitutesAddedThisMonth,
    int RegisteredUsers,
    int UsersAddedThisWeek,
    int PapersGenerated,
    int PapersGeneratedToday,
    int ActiveExams,
    int CandidatesOnline,
    ChartFeed PlatformActivity,
    ChartFeed RegionalDistribution,
    ChartFeed LicenseTierMix,
    IReadOnlyList<PendingApprovalRow> PendingApprovals,
    IReadOnlyList<ServiceHealthRow> ServiceHealth);

public sealed record PendingApprovalRow(
    string DisplayId,
    string InstituteName,
    string Type,
    DateTime SubmittedAtUtc,
    string Tier);

public sealed record ServiceHealthRow(
    string Service,
    string Status,    // "operational" | "degraded" | "down"
    string Uptime,
    string Latency);

// =============================================================================
// Institute
// =============================================================================

public sealed record InstituteDashboardSnapshot(
    int TotalStudents,
    int StudentsAddedThisMonth,
    int PapersGenerated,
    int PapersGeneratedThisMonth,
    int QuestionBankSize,
    int QuestionsAddedThisMonth,
    int ExamsScheduledOpen,
    int ExamsScheduledThisWeek,
    ChartFeed WeeklyActivity,
    ChartFeed SubjectPerformance,
    IReadOnlyList<TopContributorRow> TopContributors,
    IReadOnlyList<RecentAlertRow> RecentAlerts);

public sealed record TopContributorRow(
    string TeacherName,
    string Subject,
    int PapersAuthored,
    int ApprovedPercentage);

public sealed record RecentAlertRow(
    string Severity,           // status-badge key: "info" | "warning" | "critical" | "success"
    string Message,
    DateTime OccurredAtUtc);

// =============================================================================
// Teacher
// =============================================================================

public sealed record TeacherDashboardSnapshot(
    int PapersCreatedThisYear,
    int PapersPendingApproval,
    int PendingEvaluationsCount,
    double AverageClassScorePercent,
    double AverageClassScoreDelta,    // percentage points vs prior term; 0 if N/A
    ChartFeed ClassAverageScores,
    IReadOnlyList<RecentPaperRow> RecentPapers,
    IReadOnlyList<PendingEvaluationRow> PendingEvaluations);

public sealed record RecentPaperRow(
    string DisplayId,
    string Title,
    DateTime CreatedAtUtc,
    string Status,             // "draft" | "pending" | "approved" | "published" | "archived" | "rejected"
    int Questions,
    int Marks);

public sealed record PendingEvaluationRow(
    string StudentName,
    string ExamTitle,
    DateTime SubmittedAtUtc,
    string AnswerType,         // "Descriptive" | "Coding" | "TITA" | "Mixed"
    int QuestionCount);

// =============================================================================
// Student
// =============================================================================

public sealed record StudentDashboardSnapshot(
    int ExamsTaken,
    double AverageScorePercent,
    int? ClassRank,
    int? ClassSize,
    NextExamInfo? NextExam,
    string DisplayHeader,         // "Class XII-A · Roll No. 021 · DPS-RKP"
    ChartFeed SubjectProgress,
    IReadOnlyList<RecentResultRow> RecentResults);

public sealed record NextExamInfo(
    Guid ExamId,
    string Title,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    int TotalMarks,
    bool IsStartable);

public sealed record RecentResultRow(
    string ExamTitle,
    DateTime ExamDateUtc,
    decimal Score,
    decimal MaxScore,
    double Percentage);
