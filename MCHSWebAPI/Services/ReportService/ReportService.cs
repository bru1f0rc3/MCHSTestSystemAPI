using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Models;
using System.Text.Json;

namespace MCHSWebAPI.Services.ReportService.ReportService;

public class ReportService : IReportService
{
    private readonly IDbConnectionFactory _db;

    public ReportService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ReportDto?> GetByIdAsync(int id)
    {
        using var connection = _db.CreateConnection();
        var report = await connection.QueryFirstOrDefaultAsync<Report>(
            @"SELECT r.id, r.created_by as CreatedBy, r.report_date as ReportDate,
                     r.content::text as Content, r.created_at as CreatedAt,
                     u.username as CreatorUsername
              FROM reports r JOIN users u ON r.created_by = u.id
              WHERE r.id = @Id",
            new { Id = id });

        return report == null ? null : MapToDto(report);
    }

    public async Task<PagedResponse<ReportDto>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _db.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM reports");

        var reports = await connection.QueryAsync<Report>(
            @"SELECT r.id, r.created_by as CreatedBy, r.report_date as ReportDate,
                     r.content::text as Content, r.created_at as CreatedAt,
                     u.username as CreatorUsername
              FROM reports r JOIN users u ON r.created_by = u.id
              ORDER BY r.created_at DESC
              LIMIT @PageSize OFFSET @Offset",
            new { PageSize = pageSize, Offset = (page - 1) * pageSize });

        return new PagedResponse<ReportDto>
        {
            Items = reports.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReportDto?> CreateAsync(CreateReportRequest request, int createdBy)
    {
        using var connection = _db.CreateConnection();
        var statistics = await GetStatistics(request.DateFrom, request.DateTo, request.TestId, request.UserId);
        var userStatistics = await GetUserStatisticsForReport(request.DateFrom, request.DateTo, request.TestId);

        var reportContent = new ReportContent
        {
            Title = request.Title ?? $"Отчет от {DateTime.Now:dd.MM.yyyy}",
            Description = request.Description,
            Statistics = statistics,
            UserResults = userStatistics.ToList()
        };

        var reportId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO reports (created_by, report_date, content)
              VALUES (@CreatedBy, @ReportDate, @Content::jsonb)
              RETURNING id",
            new
            {
                CreatedBy = createdBy,
                ReportDate = DateTime.UtcNow,
                Content = JsonSerializer.Serialize(reportContent)
            });

        return await GetByIdAsync(reportId);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM reports WHERE id = @Id", new { Id = id }) > 0;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        using var connection = _db.CreateConnection();

        var dashboard = new DashboardDto
        {
            TotalUsers = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users"),
            TotalTests = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tests"),
            TotalLectures = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lectures"),
            TotalTestResults = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_results"),
            TotalCompletedTests = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM test_results WHERE finished_at IS NOT NULL"),
            AverageScore = await connection.ExecuteScalarAsync<double>(
                "SELECT COALESCE(AVG(score), 0) FROM test_results WHERE finished_at IS NOT NULL")
        };

        var completedCount = dashboard.TotalCompletedTests;
        if (completedCount > 0)
        {
            var passedCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM test_results WHERE score >= 70 AND finished_at IS NOT NULL");
            dashboard.OverallPassRate = (passedCount * 100.0) / completedCount;
        }

        dashboard.RecentActivity = (await connection.QueryAsync<RecentActivityDto>(
            @"SELECT u.username as Username, t.title as TestTitle, tr.score,
                     CASE WHEN tr.score >= 70 THEN 'passed' ELSE 'failed' END as Status,
                     tr.finished_at as CompletedAt
              FROM test_results tr
              JOIN users u ON tr.user_id = u.id
              JOIN tests t ON tr.test_id = t.id
              WHERE tr.finished_at IS NOT NULL
              ORDER BY tr.finished_at DESC LIMIT 10")).ToList();

        return dashboard;
    }

    public async Task<IEnumerable<TestStatisticsDto>> GetTestStatisticsAsync()
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryAsync<TestStatisticsDto>(
            @"SELECT
                t.id as TestId, t.title as TestTitle,
                COUNT(tr.id) as TotalAttempts,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as CompletedAttempts,
                COUNT(CASE WHEN tr.score >= 70 AND tr.finished_at IS NOT NULL THEN 1 END) as PassedAttempts,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as FailedAttempts,
                COALESCE(AVG(CASE WHEN tr.finished_at IS NOT NULL THEN tr.score END), 0) as AverageScore,
                CASE
                    WHEN COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) > 0
                    THEN (COUNT(CASE WHEN tr.score >= 70 AND tr.finished_at IS NOT NULL THEN 1 END) * 100.0 /
                          COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END))
                    ELSE 0
                END as PassRate
              FROM tests t
              LEFT JOIN test_results tr ON t.id = tr.test_id
              GROUP BY t.id, t.title
              ORDER BY t.title");
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(int userId)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserStatisticsDto>(
            @"SELECT
                u.id as UserId, u.username as Username,
                COUNT(tr.id) as TotalTestsTaken,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as TestsCompleted,
                COUNT(CASE WHEN tr.score >= 70 AND tr.finished_at IS NOT NULL THEN 1 END) as TestsPassed,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as TestsFailed,
                COALESCE(AVG(CASE WHEN tr.finished_at IS NOT NULL THEN tr.score END), 0) as AverageScore,
                CASE
                    WHEN COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) > 0
                    THEN (COUNT(CASE WHEN tr.score >= 70 AND tr.finished_at IS NOT NULL THEN 1 END) * 100.0 /
                          COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END))
                    ELSE 0
                END as PassRate
              FROM users u
              LEFT JOIN test_results tr ON u.id = tr.user_id
              WHERE u.id = @UserId
              GROUP BY u.id, u.username",
            new { UserId = userId });
    }

    public async Task<PagedResponse<UserPerformanceDto>> GetUsersPerformanceAsync(int page, int pageSize)
    {
        using var connection = _db.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT u.id) FROM users u INNER JOIN test_results tr ON u.id = tr.user_id");

        var users = await connection.QueryAsync<UserPerformanceDto>(
            @"SELECT
                u.id as UserId, u.username as Username,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as TestsCompleted,
                COALESCE(AVG(CASE WHEN tr.finished_at IS NOT NULL THEN tr.score END), 0) as AverageScore,
                CASE
                    WHEN COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) > 0
                    THEN (COUNT(CASE WHEN tr.score >= 70 AND tr.finished_at IS NOT NULL THEN 1 END) * 100.0 /
                          COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END))
                    ELSE 0
                END as PassRate,
                MAX(tr.finished_at) as LastActivity
              FROM users u
              LEFT JOIN test_results tr ON u.id = tr.user_id
              GROUP BY u.id, u.username
              HAVING COUNT(tr.id) > 0
              ORDER BY TestsCompleted DESC, AverageScore DESC
              LIMIT @PageSize OFFSET @Offset",
            new { PageSize = pageSize, Offset = (page - 1) * pageSize });

        return new PagedResponse<UserPerformanceDto>
        {
            Items = users.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DetailedReportDto> GetDetailedReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int? userId,
        int? testId)
    {
        using var connection = _db.CreateConnection();

        var where = new List<string>();
        var p = new DynamicParameters();

        if (startDate.HasValue)
        {
            where.Add("tr.started_at >= @StartDate");
            p.Add("StartDate", startDate.Value);
        }
        if (endDate.HasValue)
        {
            where.Add("tr.started_at <= @EndDate");
            p.Add("EndDate", endDate.Value);
        }
        if (userId.HasValue)
        {
            where.Add("tr.user_id = @UserId");
            p.Add("UserId", userId.Value);
        }
        if (testId.HasValue)
        {
            where.Add("tr.test_id = @TestId");
            p.Add("TestId", testId.Value);
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;

        var rows = (await connection.QueryAsync<TestResultSummaryDto>(
            $@"SELECT
                   tr.id         AS Id,
                   u.username    AS Username,
                   u.email       AS Email,
                   TRIM(BOTH ' ' FROM
                       COALESCE(u.last_name,  '') || ' ' ||
                       COALESCE(u.first_name, '') || ' ' ||
                       COALESCE(u.patronymic, '')
                   ) AS FullName,
                   t.title       AS TestTitle,
                   tr.started_at   AS StartedAt,
                   tr.finished_at  AS FinishedAt,
                   tr.score      AS Score,
                   t.passing_score AS PassingScore,
                   t.time_limit_minutes AS TimeLimitMinutes,
                   CASE
                       WHEN tr.finished_at IS NOT NULL
                       THEN EXTRACT(EPOCH FROM (tr.finished_at - tr.started_at))::INT
                       ELSE NULL
                   END AS DurationSeconds,
                   tr.cheat_attempts AS CheatAttempts,
                   tr.auto_submitted AS AutoSubmitted,
                   CASE
                       WHEN tr.finished_at IS NULL THEN 'in_progress'
                       WHEN tr.score >= t.passing_score THEN 'passed'
                       ELSE 'failed'
                   END AS Status
               FROM test_results tr
               JOIN users u ON tr.user_id = u.id
               JOIN tests t ON tr.test_id = t.id
               {whereClause}
               ORDER BY tr.started_at DESC
               LIMIT 2000",
            p)).ToList();
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.FullName)) r.FullName = null;
            if (string.IsNullOrWhiteSpace(r.Email)) r.Email = null;
        }

        var completed = rows.Where(r => r.FinishedAt != null && r.Score.HasValue).ToList();
        var avg = completed.Count > 0 ? completed.Average(r => r.Score!.Value) : 0.0;
        var passed = completed.Count(r => r.Score!.Value >= r.PassingScore);
        var failed = completed.Count - passed;
        var passRate = completed.Count > 0 ? (passed * 100.0 / completed.Count) : 0.0;
        var avgDuration = completed.Where(r => r.DurationSeconds.HasValue)
            .Select(r => r.DurationSeconds!.Value)
            .DefaultIfEmpty(0)
            .Average();
        var totalCheat = rows.Sum(r => r.CheatAttempts);
        string? testTitle = null;
        if (testId.HasValue)
        {
            testTitle = await connection.ExecuteScalarAsync<string?>(
                "SELECT title FROM tests WHERE id = @Id", new { Id = testId.Value });
        }

        return new DetailedReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            TestId = testId,
            TestTitle = testTitle,
            Kind = ResolveKind(startDate, endDate, testId),
            TotalResults = rows.Count,
            CompletedResults = completed.Count,
            PassedResults = passed,
            FailedResults = failed,
            TotalCheatAttempts = totalCheat,
            AverageScore = Math.Round(avg, 2),
            PassRate = Math.Round(passRate, 2),
            AverageDurationSeconds = Math.Round(avgDuration, 0),
            Results = rows
        };
    }

    private static string ResolveKind(DateTime? start, DateTime? end, int? testId)
    {
        bool hasTest = testId.HasValue;
        bool isSameDay = start.HasValue && end.HasValue &&
                         start.Value.Date == end.Value.Date;
        bool hasPeriod = (start.HasValue || end.HasValue) && !isSameDay;

        if (hasTest && hasPeriod) return "test_period";
        if (hasTest) return "test";
        if (isSameDay) return "date";
        if (hasPeriod) return "period";
        return "all";
    }

    private async Task<ReportStatistics> GetStatistics(DateTime? from, DateTime? to, int? testId, int? userId)
    {
        using var connection = _db.CreateConnection();

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (from.HasValue) { whereClauses.Add("DATE(tr.started_at) >= @From"); parameters.Add("From", from.Value.Date); }
        if (to.HasValue) { whereClauses.Add("DATE(tr.started_at) <= @To"); parameters.Add("To", to.Value.Date); }
        if (testId.HasValue) { whereClauses.Add("tr.test_id = @TestId"); parameters.Add("TestId", testId.Value); }
        if (userId.HasValue) { whereClauses.Add("tr.user_id = @UserId"); parameters.Add("UserId", userId.Value); }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        return await connection.QueryFirstAsync<ReportStatistics>(
            $@"SELECT
                COUNT(DISTINCT tr.test_id) as TotalTests,
                COUNT(tr.id) as TotalAttempts,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as CompletedAttempts,
                COALESCE(AVG(tr.score), 0) as AverageScore,
                COUNT(CASE WHEN tr.score >= 70 THEN 1 END) as PassedCount,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as FailedCount
              FROM test_results tr {whereClause}", parameters);
    }

    private async Task<IEnumerable<UserStatistics>> GetUserStatisticsForReport(DateTime? from, DateTime? to, int? testId)
    {
        using var connection = _db.CreateConnection();

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (from.HasValue) { whereClauses.Add("DATE(tr.started_at) >= @From"); parameters.Add("From", from.Value.Date); }
        if (to.HasValue) { whereClauses.Add("DATE(tr.started_at) <= @To"); parameters.Add("To", to.Value.Date); }
        if (testId.HasValue) { whereClauses.Add("tr.test_id = @TestId"); parameters.Add("TestId", testId.Value); }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        return await connection.QueryAsync<UserStatistics>(
            $@"SELECT
                u.id as UserId, u.username as Username,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as TestsCompleted,
                COALESCE(AVG(tr.score), 0) as AverageScore,
                COUNT(CASE WHEN tr.score >= 70 THEN 1 END) as PassedCount,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as FailedCount
              FROM users u
              LEFT JOIN test_results tr ON u.id = tr.user_id
              {whereClause}
              GROUP BY u.id, u.username
              HAVING COUNT(tr.id) > 0
              ORDER BY u.username", parameters);
    }

    private static ReportDto MapToDto(Report report)
    {
        ReportContent? content = null;
        if (!string.IsNullOrEmpty(report.Content))
        {
            try { content = JsonSerializer.Deserialize<ReportContent>(report.Content); }
            catch {  }
        }

        return new ReportDto
        {
            Id = report.Id,
            CreatorUsername = report.CreatorUsername ?? "unknown",
            ReportDate = report.ReportDate,
            Content = content,
            CreatedAt = report.CreatedAt
        };
    }
}
