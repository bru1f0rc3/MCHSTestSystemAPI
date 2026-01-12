using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class UserAnswerRepository : IUserAnswerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserAnswerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserAnswer?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, test_result_id as TestResultId, question_id as QuestionId, 
                   answer_id as AnswerId, answered_at as AnsweredAt
            FROM user_answers WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<UserAnswer>(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserAnswer>> GetByTestResultIdAsync(int testResultId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT ua.id, ua.test_result_id as TestResultId, ua.question_id as QuestionId, 
                   ua.answer_id as AnswerId, ua.answered_at as AnsweredAt,
                   q.question_text as QuestionText, a.answer_text as AnswerText, a.is_correct as IsCorrect
            FROM user_answers ua
            JOIN questions q ON ua.question_id = q.id
            LEFT JOIN answers a ON ua.answer_id = a.id
            WHERE ua.test_result_id = @TestResultId
            ORDER BY q.position";
        return await connection.QueryAsync<UserAnswer>(sql, new { TestResultId = testResultId });
    }

    public async Task<UserAnswer?> GetByTestResultAndQuestionAsync(int testResultId, int questionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, test_result_id as TestResultId, question_id as QuestionId, 
                   answer_id as AnswerId, answered_at as AnsweredAt
            FROM user_answers 
            WHERE test_result_id = @TestResultId AND question_id = @QuestionId";
        return await connection.QueryFirstOrDefaultAsync<UserAnswer>(sql, new { TestResultId = testResultId, QuestionId = questionId });
    }

    public async Task<int> CreateAsync(UserAnswer userAnswer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO user_answers (test_result_id, question_id, answer_id, answered_at)
            VALUES (@TestResultId, @QuestionId, @AnswerId, @AnsweredAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            userAnswer.TestResultId,
            userAnswer.QuestionId,
            userAnswer.AnswerId,
            AnsweredAt = DateTime.UtcNow
        });
    }

    public async Task<bool> UpdateAsync(UserAnswer userAnswer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE user_answers 
            SET answer_id = @AnswerId, answered_at = @AnsweredAt
            WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new
        {
            userAnswer.Id,
            userAnswer.AnswerId,
            AnsweredAt = DateTime.UtcNow
        });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM user_answers WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<int> GetCorrectCountByTestResultIdAsync(int testResultId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(*) 
            FROM user_answers ua
            JOIN answers a ON ua.answer_id = a.id
            WHERE ua.test_result_id = @TestResultId AND a.is_correct = true";
        return await connection.ExecuteScalarAsync<int>(sql, new { TestResultId = testResultId });
    }
}
