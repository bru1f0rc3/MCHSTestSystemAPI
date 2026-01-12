using Dapper;
using MCHSWebAPI.DTOs.Common;
using MCHSWebAPI.DTOs.Reports;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;
using System.Text.Json;

namespace MCHSWebAPI.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Report?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT r.id, r.created_by as CreatedBy, r.report_date as ReportDate, 
                   r.content::text as Content, r.created_at as CreatedAt,
                   u.username as CreatorUsername
            FROM reports r
            JOIN users u ON r.created_by = u.id
            WHERE r.id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Report>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Report>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT r.id, r.created_by as CreatedBy, r.report_date as ReportDate, 
                   r.content::text as Content, r.created_at as CreatedAt,
                   u.username as CreatorUsername
            FROM reports r
            JOIN users u ON r.created_by = u.id
            ORDER BY r.created_at DESC
            LIMIT @PageSize OFFSET @Offset";
        return await connection.QueryAsync<Report>(sql, new { PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<IEnumerable<Report>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT r.id, r.created_by as CreatedBy, r.report_date as ReportDate, 
                   r.content::text as Content, r.created_at as CreatedAt,
                   u.username as CreatorUsername
            FROM reports r
            JOIN users u ON r.created_by = u.id
            WHERE r.report_date BETWEEN @From AND @To
            ORDER BY r.report_date DESC";
        return await connection.QueryAsync<Report>(sql, new { From = from, To = to });
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM reports");
    }

    public async Task<int> CreateAsync(Report report)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO reports (created_by, report_date, content)
            VALUES (@CreatedBy, @ReportDate, @Content::jsonb)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, report);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM reports WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        var dashboard = new DashboardDto();

        dashboard.TotalUsers = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
        dashboard.TotalTests = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tests");
        dashboard.TotalLectures = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lectures");
        dashboard.TotalTestResults = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_results");

        dashboard.TotalCompletedTests = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM test_results 
              WHERE finished_at IS NOT NULL");

        dashboard.AverageScore = await connection.ExecuteScalarAsync<double>(
            @"SELECT COALESCE(AVG(score), 0) 
              FROM test_results 
              WHERE finished_at IS NOT NULL");

        var completedCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM test_results WHERE finished_at IS NOT NULL");

        if (completedCount > 0)
        {
            var passedCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM test_results WHERE score >= 70 AND finished_at IS NOT NULL");
            dashboard.OverallPassRate = (passedCount * 100.0) / completedCount;
        }
        else
        {
            dashboard.OverallPassRate = 0;
        }

        const string recentSql = @"
            SELECT u.username as Username, t.title as TestTitle, tr.score, 
                   CASE WHEN tr.score >= 70 THEN 'passed' ELSE 'failed' END as Status,
                   tr.finished_at as CompletedAt
            FROM test_results tr
            JOIN users u ON tr.user_id = u.id
            JOIN tests t ON tr.test_id = t.id
            WHERE tr.finished_at IS NOT NULL
            ORDER BY tr.finished_at DESC
            LIMIT 10";
        dashboard.RecentActivity = (await connection.QueryAsync<RecentActivityDto>(recentSql)).ToList();

        return dashboard;
    }

    public async Task<IEnumerable<TestStatisticsDto>> GetTestStatisticsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                t.id as TestId,
                t.title as TestTitle,
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
            ORDER BY t.title";
        return await connection.QueryAsync<TestStatisticsDto>(sql);
    }

    public async Task<ReportStatistics> GetStatisticsAsync(DateOnly? from, DateOnly? to, int? testId, int? userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (from.HasValue)
        {
            whereClauses.Add("DATE(tr.started_at) >= @From");
            parameters.Add("From", from.Value);
        }
        if (to.HasValue)
        {
            whereClauses.Add("DATE(tr.started_at) <= @To");
            parameters.Add("To", to.Value);
        }
        if (testId.HasValue)
        {
            whereClauses.Add("tr.test_id = @TestId");
            parameters.Add("TestId", testId.Value);
        }
        if (userId.HasValue)
        {
            whereClauses.Add("tr.user_id = @UserId");
            parameters.Add("UserId", userId.Value);
        }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var sql = $@"
            SELECT 
                COUNT(DISTINCT tr.test_id) as TotalTests,
                COUNT(tr.id) as TotalAttempts,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as CompletedAttempts,
                COALESCE(AVG(tr.score), 0) as AverageScore,
                COUNT(CASE WHEN tr.score >= 70 THEN 1 END) as PassedCount,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as FailedCount
            FROM test_results tr
            {whereClause}";

        return await connection.QueryFirstAsync<ReportStatistics>(sql, parameters);
    }

    public async Task<IEnumerable<UserStatistics>> GetUserStatisticsAsync(DateOnly? from, DateOnly? to, int? testId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (from.HasValue)
        {
            whereClauses.Add("DATE(tr.started_at) >= @From");
            parameters.Add("From", from.Value);
        }
        if (to.HasValue)
        {
            whereClauses.Add("DATE(tr.started_at) <= @To");
            parameters.Add("To", to.Value);
        }
        if (testId.HasValue)
        {
            whereClauses.Add("tr.test_id = @TestId");
            parameters.Add("TestId", testId.Value);
        }

        var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var sql = $@"
            SELECT 
                u.id as UserId,
                u.username as Username,
                COUNT(CASE WHEN tr.finished_at IS NOT NULL THEN 1 END) as TestsCompleted,
                COALESCE(AVG(tr.score), 0) as AverageScore,
                COUNT(CASE WHEN tr.score >= 70 THEN 1 END) as PassedCount,
                COUNT(CASE WHEN tr.score < 70 AND tr.finished_at IS NOT NULL THEN 1 END) as FailedCount
            FROM users u
            LEFT JOIN test_results tr ON u.id = tr.user_id
            {whereClause}
            GROUP BY u.id, u.username
            HAVING COUNT(tr.id) > 0
            ORDER BY u.username";

        return await connection.QueryAsync<UserStatistics>(sql, parameters);
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsByIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                u.id as UserId,
                u.username as Username,
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
            GROUP BY u.id, u.username";

        return await connection.QueryFirstOrDefaultAsync<UserStatisticsDto>(sql, new { UserId = userId });
    }
    public async Task<PagedResponse<UserPerformanceDto>> GetUsersPerformanceAsync(int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT u.id) 
              FROM users u 
              INNER JOIN test_results tr ON u.id = tr.user_id");

        var sql = @"
            SELECT 
                u.id as UserId,
                u.username as Username,
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
            LIMIT @PageSize OFFSET @Offset";

        var users = await connection.QueryAsync<UserPerformanceDto>(sql, new
        {
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });

        return new PagedResponse<UserPerformanceDto>
        {
            Items = users.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
