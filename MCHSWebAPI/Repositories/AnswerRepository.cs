using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AnswerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Answer?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, question_id as QuestionId, answer_text as AnswerText, is_correct as IsCorrect, position
            FROM answers WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Answer>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, question_id as QuestionId, answer_text as AnswerText, is_correct as IsCorrect, position
            FROM answers WHERE question_id = @QuestionId ORDER BY position";
        return await connection.QueryAsync<Answer>(sql, new { QuestionId = questionId });
    }

    public async Task<Answer?> GetCorrectAnswerByQuestionIdAsync(int questionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, question_id as QuestionId, answer_text as AnswerText, is_correct as IsCorrect, position
            FROM answers WHERE question_id = @QuestionId AND is_correct = true LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<Answer>(sql, new { QuestionId = questionId });
    }

    public async Task<Dictionary<int, Answer>> GetCorrectAnswersByQuestionIdsAsync(IEnumerable<int> questionIds)
    {
        var ids = questionIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, Answer>();

        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, question_id as QuestionId, answer_text as AnswerText, is_correct as IsCorrect, position
            FROM answers WHERE question_id = ANY(@Ids) AND is_correct = true";
        
        var answers = await connection.QueryAsync<Answer>(sql, new { Ids = ids.ToArray() });
        return answers.ToDictionary(a => a.QuestionId, a => a);
    }

    public async Task<int> CreateAsync(Answer answer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO answers (question_id, answer_text, is_correct, position)
            VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, answer);
    }

    public async Task<bool> UpdateAsync(Answer answer)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE answers 
            SET answer_text = @AnswerText, is_correct = @IsCorrect, position = @Position
            WHERE id = @Id";
        return await connection.ExecuteAsync(sql, answer) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM answers WHERE id = @Id", new { Id = id }) > 0;
    }

    public async Task<bool> DeleteByQuestionIdAsync(int questionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM answers WHERE question_id = @QuestionId", new { QuestionId = questionId });
        return true;
    }
}
