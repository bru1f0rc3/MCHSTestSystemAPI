using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class TestResultRepository : ITestResultRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TestResultRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TestResult?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, user_id as UserId, test_id as TestId, started_at as StartedAt, 
                   finished_at as FinishedAt, score
            FROM test_results WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<TestResult>(sql, new { Id = id });
    }

    public async Task<TestResult?> GetByIdWithDetailsAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT tr.id, tr.user_id as UserId, tr.test_id as TestId, tr.started_at as StartedAt, 
                   tr.finished_at as FinishedAt, tr.score,
                   u.username as Username, t.title as TestTitle,
                   CASE 
                       WHEN tr.finished_at IS NULL THEN 'in_progress'
                       WHEN tr.score >= 70 THEN 'passed'
                       ELSE 'failed'
                   END as Status
            FROM test_results tr
            JOIN users u ON tr.user_id = u.id
            JOIN tests t ON tr.test_id = t.id
            WHERE tr.id = @Id";
        return await connection.QueryFirstOrDefaultAsync<TestResult>(sql, new { Id = id });
    }

    public async Task<IEnumerable<TestResult>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT tr.id, tr.user_id as UserId, tr.test_id as TestId, tr.started_at as StartedAt, 
                   tr.finished_at as FinishedAt, tr.score,
                   u.username as Username, t.title as TestTitle,
                   CASE 
                       WHEN tr.finished_at IS NULL THEN 'in_progress'
                       WHEN tr.score >= 70 THEN 'passed'
                       ELSE 'failed'
                   END as Status
            FROM test_results tr
            JOIN users u ON tr.user_id = u.id
            JOIN tests t ON tr.test_id = t.id
            WHERE tr.user_id = @UserId
            ORDER BY tr.started_at DESC
            LIMIT @PageSize OFFSET @Offset";
        return await connection.QueryAsync<TestResult>(sql, new { UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<IEnumerable<TestResult>> GetByTestIdAsync(int testId, int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT tr.id, tr.user_id as UserId, tr.test_id as TestId, tr.started_at as StartedAt, 
                   tr.finished_at as FinishedAt, tr.score,
                   u.username as Username, t.title as TestTitle,
                   CASE 
                       WHEN tr.finished_at IS NULL THEN 'in_progress'
                       WHEN tr.score >= 70 THEN 'passed'
                       ELSE 'failed'
                   END as Status
            FROM test_results tr
            JOIN users u ON tr.user_id = u.id
            JOIN tests t ON tr.test_id = t.id
            WHERE tr.test_id = @TestId
            ORDER BY tr.started_at DESC
            LIMIT @PageSize OFFSET @Offset";
        return await connection.QueryAsync<TestResult>(sql, new { TestId = testId, PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<IEnumerable<TestResult>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT tr.id, tr.user_id as UserId, tr.test_id as TestId, tr.started_at as StartedAt, 
                   tr.finished_at as FinishedAt, tr.score,
                   u.username as Username, t.title as TestTitle,
                   CASE 
                       WHEN tr.finished_at IS NULL THEN 'in_progress'
                       WHEN tr.score >= 70 THEN 'passed'
                       ELSE 'failed'
                   END as Status
            FROM test_results tr
            JOIN users u ON tr.user_id = u.id
            JOIN tests t ON tr.test_id = t.id
            ORDER BY tr.started_at DESC
            LIMIT @PageSize OFFSET @Offset";
        return await connection.QueryAsync<TestResult>(sql, new { PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<TestResult?> GetInProgressByUserAndTestAsync(int userId, int testId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, user_id as UserId, test_id as TestId, started_at as StartedAt, 
                   finished_at as FinishedAt, score
            FROM test_results 
            WHERE user_id = @UserId AND test_id = @TestId AND finished_at IS NULL
            ORDER BY started_at DESC LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<TestResult>(sql, new { UserId = userId, TestId = testId });
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_results");
    }

    public async Task<int> GetCountByUserIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM test_results WHERE user_id = @UserId", new { UserId = userId });
    }

    public async Task<int> CreateAsync(TestResult testResult)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO test_results (user_id, test_id, started_at)
            VALUES (@UserId, @TestId, @StartedAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, new 
        { 
            testResult.UserId, 
            testResult.TestId, 
            StartedAt = DateTime.UtcNow 
        });
    }

    public async Task<bool> UpdateAsync(TestResult testResult)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE test_results 
            SET finished_at = @FinishedAt, score = @Score
            WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, testResult);
        return affected > 0;
    }

    public async Task<bool> FinishTestAsync(int id, double score)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE test_results 
            SET finished_at = @FinishedAt, score = @Score
            WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, FinishedAt = DateTime.UtcNow, Score = score });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM test_results WHERE id = @Id", new { Id = id });
        return affected > 0;
    }
}
