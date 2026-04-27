namespace MCHSWebAPI.DTOs;

public class ReportDto
{
    public int Id { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public ReportContent? Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportContent
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatistics Statistics { get; set; } = new();
    public List<UserStatistics> UserResults { get; set; } = new();
}

public class ReportStatistics
{
    public int TotalTests { get; set; }
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public double AverageScore { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
}

public class UserStatistics
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TestsCompleted { get; set; }
    public double AverageScore { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
}

public class CreateReportRequest
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? TestId { get; set; }
    public int? UserId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class TestStatisticsDto
{
    public int TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public int PassedAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
}

public class DashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalTests { get; set; }
    public int TotalLectures { get; set; }
    public int TotalTestResults { get; set; }
    public int TotalCompletedTests { get; set; }
    public double AverageScore { get; set; }
    public double OverallPassRate { get; set; }
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class RecentActivityDto
{
    public string Username { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public double? Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class UserStatisticsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalTestsTaken { get; set; }
    public int TestsCompleted { get; set; }
    public int TestsPassed { get; set; }
    public int TestsFailed { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
}

public class UserPerformanceDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TestsCompleted { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public DateTime? LastActivity { get; set; }
}
public class DetailedReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TestId { get; set; }
    public string? TestTitle { get; set; }
    public string Kind { get; set; } = "all";
    public int TotalResults { get; set; }
    public int CompletedResults { get; set; }
    public int PassedResults { get; set; }
    public int FailedResults { get; set; }
    public int TotalCheatAttempts { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double AverageDurationSeconds { get; set; }
    public List<TestResultSummaryDto> Results { get; set; } = new();
}
public class TestResultSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double? Score { get; set; }
    public int PassingScore { get; set; } = 70;
    public int? TimeLimitMinutes { get; set; }
    public int? DurationSeconds { get; set; }
    public int CheatAttempts { get; set; }
    public bool AutoSubmitted { get; set; }
    public string Status { get; set; } = string.Empty;
}
