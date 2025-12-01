using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.TestResults
{
    public class TestResultService
    {
        private readonly DBConnect _dbConnect;

        public TestResultService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<TestResult>> GetResultsByUserIdAsync(int userId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM test_results WHERE user_id = @UserId ORDER BY started_at DESC";
            return await connection.QueryAsync<TestResult>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<TestResult>> GetResultsByTestIdAsync(int testId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM test_results WHERE test_id = @TestId ORDER BY started_at DESC";
            return await connection.QueryAsync<TestResult>(sql, new { TestId = testId });
        }

        public async Task<TestResult?> GetResultByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM test_results WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<TestResult>(sql, new { Id = id });
        }

        public async Task<int> StartTestAsync(int userId, int testId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO test_results (user_id, test_id, started_at, score) 
                       VALUES (@UserId, @TestId, @StartedAt, 0) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, 
                new { UserId = userId, TestId = testId, StartedAt = DateTime.UtcNow });
        }

        public async Task<bool> FinishTestAsync(int resultId, double score)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE test_results 
                       SET finished_at = @FinishedAt, score = @Score 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, 
                new { Id = resultId, FinishedAt = DateTime.UtcNow, Score = score });
            return rows > 0;
        }

        public async Task<bool> SaveUserAnswerAsync(UserAnswer userAnswer)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO user_answers (test_result_id, question_id, answer_id, answered_at) 
                       VALUES (@TestResultId, @QuestionId, @AnswerId, @AnsweredAt)";
            var rows = await connection.ExecuteAsync(sql, userAnswer);
            return rows > 0;
        }

        public async Task<IEnumerable<UserAnswer>> GetUserAnswersByResultIdAsync(int resultId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM user_answers WHERE test_result_id = @ResultId";
            return await connection.QueryAsync<UserAnswer>(sql, new { ResultId = resultId });
        }

        public async Task<double> CalculateTestScoreAsync(int resultId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"
                SELECT COUNT(*) FILTER (WHERE a.is_correct = true) * 100.0 / COUNT(*)
                FROM user_answers ua
                JOIN answers a ON ua.answer_id = a.id
                WHERE ua.test_result_id = @ResultId";
            return await connection.ExecuteScalarAsync<double>(sql, new { ResultId = resultId });
        }
    }
}
