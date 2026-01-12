using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public QuestionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Question?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, test_id as TestId, question_text as QuestionText, position
            FROM questions WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Question>(sql, new { Id = id });
    }

    public async Task<Question?> GetByIdWithAnswersAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var question = await GetByIdAsync(id);
        if (question == null) return null;

        const string sql = @"
            SELECT id, question_id as QuestionId, answer_text as AnswerText, is_correct as IsCorrect, position
            FROM answers WHERE question_id = @QuestionId ORDER BY position";
        question.Answers = (await connection.QueryAsync<Answer>(sql, new { QuestionId = id })).ToList();

        return question;
    }

    public async Task<IEnumerable<Question>> GetByTestIdAsync(int testId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, test_id as TestId, question_text as QuestionText, position
            FROM questions WHERE test_id = @TestId ORDER BY position";
        return await connection.QueryAsync<Question>(sql, new { TestId = testId });
    }

    public async Task<int> CreateAsync(Question question)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO questions (test_id, question_text, position)
            VALUES (@TestId, @QuestionText, @Position)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, question);
    }

    public async Task<bool> UpdateAsync(Question question)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE questions 
            SET question_text = @QuestionText, position = @Position
            WHERE id = @Id";
        return await connection.ExecuteAsync(sql, question) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM questions WHERE id = @Id", new { Id = id }) > 0;
    }

    public async Task<int> GetCountByTestIdAsync(int testId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM questions WHERE test_id = @TestId", new { TestId = testId });
    }

    public async Task<Dictionary<int, int>> GetCountsByTestIdsAsync(IEnumerable<int> testIds)
    {
        var idsList = testIds.ToList();
        if (idsList.Count == 0) return new Dictionary<int, int>();

        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT test_id as TestId, COUNT(*) as Count
            FROM questions 
            WHERE test_id = ANY(@TestIds)
            GROUP BY test_id";
        
        var results = await connection.QueryAsync<(int TestId, int Count)>(sql, new { TestIds = idsList.ToArray() });
        return results.ToDictionary(r => r.TestId, r => r.Count);
    }
}
