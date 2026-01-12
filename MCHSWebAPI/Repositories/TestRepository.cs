using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class TestRepository : ITestRepository
{
    private readonly IDbConnectionFactory _db;

    public TestRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Test?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT t.id, t.lecture_id as LectureId, t.title, t.description, 
                   t.created_by as CreatedBy, t.created_at as CreatedAt,
                   l.title as LectureTitle, u.username as CreatorUsername
            FROM tests t
            LEFT JOIN lectures l ON t.lecture_id = l.id
            JOIN users u ON t.created_by = u.id
            WHERE t.id = @Id";
        return await conn.QueryFirstOrDefaultAsync<Test>(sql, new { Id = id });
    }

    public async Task<Test?> GetByIdWithQuestionsAsync(int id)
    {
        using var conn = _db.CreateConnection();

        var test = await GetByIdAsync(id);
        if (test == null) return null;

        const string qSql = @"
            SELECT id, test_id as TestId, question_text as QuestionText, position
            FROM questions WHERE test_id = @TestId ORDER BY position";
        var questions = (await conn.QueryAsync<Question>(qSql, new { TestId = id })).ToList();

        var qIds = questions.Select(q => q.Id).ToArray();
        if (qIds.Length > 0)
        {
            const string aSql = @"
                SELECT id, question_id as QuestionId, answer_text as AnswerText, 
                       is_correct as IsCorrect, position
                FROM answers WHERE question_id = ANY(@Ids) ORDER BY position";
            var answers = await conn.QueryAsync<Answer>(aSql, new { Ids = qIds });
            var lookup = answers.ToLookup(a => a.QuestionId);
            foreach (var q in questions)
                q.Answers = lookup[q.Id].ToList();
        }

        test.Questions = questions;
        return test;
    }

    public async Task<IEnumerable<Test>> GetAllAsync(int page, int pageSize)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT t.id, t.lecture_id as LectureId, t.title, t.description, 
                   t.created_by as CreatedBy, t.created_at as CreatedAt,
                   l.title as LectureTitle, u.username as CreatorUsername
            FROM tests t
            LEFT JOIN lectures l ON t.lecture_id = l.id
            JOIN users u ON t.created_by = u.id
            ORDER BY t.created_at DESC
            LIMIT @Limit OFFSET @Offset";
        return await conn.QueryAsync<Test>(sql, new { Limit = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<IEnumerable<Test>> GetByLectureIdAsync(int lectureId)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT t.id, t.lecture_id as LectureId, t.title, t.description, 
                   t.created_by as CreatedBy, t.created_at as CreatedAt,
                   l.title as LectureTitle, u.username as CreatorUsername
            FROM tests t
            LEFT JOIN lectures l ON t.lecture_id = l.id
            JOIN users u ON t.created_by = u.id
            WHERE t.lecture_id = @LectureId
            ORDER BY t.created_at DESC";
        return await conn.QueryAsync<Test>(sql, new { LectureId = lectureId });
    }

    public async Task<IEnumerable<Test>> GetAvailableForUserAsync(int userId, int page, int pageSize)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT t.id, t.lecture_id as LectureId, t.title, t.description, 
                   t.created_by as CreatedBy, t.created_at as CreatedAt,
                   l.title as LectureTitle, u.username as CreatorUsername
            FROM tests t
            LEFT JOIN lectures l ON t.lecture_id = l.id
            JOIN users u ON t.created_by = u.id
            WHERE NOT EXISTS (
                SELECT 1 FROM test_results tr 
                WHERE tr.test_id = t.id AND tr.user_id = @UserId 
                  AND tr.finished_at IS NOT NULL AND tr.score >= 70
            )
            ORDER BY t.created_at DESC
            LIMIT @Limit OFFSET @Offset";
        return await conn.QueryAsync<Test>(sql, new { UserId = userId, Limit = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tests");
    }

    public async Task<int> GetAvailableCountForUserAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            SELECT COUNT(*) FROM tests t
            WHERE NOT EXISTS (
                SELECT 1 FROM test_results tr 
                WHERE tr.test_id = t.id AND tr.user_id = @UserId 
                  AND tr.finished_at IS NOT NULL AND tr.score >= 70
            )";
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<int> CreateAsync(Test test)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO tests (lecture_id, title, description, created_by)
            VALUES (@LectureId, @Title, @Description, @CreatedBy)
            RETURNING id";
        return await conn.ExecuteScalarAsync<int>(sql, test);
    }

    public async Task<bool> UpdateAsync(Test test)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE tests 
            SET lecture_id = @LectureId, title = @Title, description = @Description
            WHERE id = @Id";
        return await conn.ExecuteAsync(sql, test) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM tests WHERE id = @Id", new { Id = id }) > 0;
    }
}
