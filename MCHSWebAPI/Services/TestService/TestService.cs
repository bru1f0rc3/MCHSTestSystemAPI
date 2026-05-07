using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.TestService.TestService;

public class TestService : ITestService
{
    private readonly IDbConnectionFactory _db;
    private readonly IPdfParserService _pdfParserService;

    private const string TestBaseColumns = @"
        t.id, t.lecture_id AS LectureId, t.title, t.description,
        t.time_limit_minutes AS TimeLimitMinutes, t.passing_score AS PassingScore,
        t.created_by AS CreatedBy, t.created_at AS CreatedAt,
        l.title AS LectureTitle, u.username AS CreatorUsername";

    private const string TestBaseFrom = @"
        FROM tests t
        LEFT JOIN lectures l ON t.lecture_id = l.id
        JOIN users u ON t.created_by = u.id";

    public TestService(IDbConnectionFactory db, IPdfParserService pdfParserService)
    {
        _db = db;
        _pdfParserService = pdfParserService;
    }

    public async Task<TestDto?> GetByIdAsync(int id)
    {
        using var connection = _db.CreateConnection();
        var sql = $"SELECT {TestBaseColumns} {TestBaseFrom} WHERE t.id = @Id";

        var test = await connection.QueryFirstOrDefaultAsync<Test>(sql, new { Id = id });
        if (test == null) return null;

        var questionsCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM questions WHERE test_id = @Id", new { Id = id });

        return ToDto(test, questionsCount);
    }

    public async Task<TestDetailDto?> GetByIdWithQuestionsAsync(int id, bool includeCorrectAnswers)
    {
        using var connection = _db.CreateConnection();
        var sql = $"SELECT {TestBaseColumns} {TestBaseFrom} WHERE t.id = @Id";

        var test = await connection.QueryFirstOrDefaultAsync<Test>(sql, new { Id = id });
        if (test == null) return null;

        var questions = (await connection.QueryAsync<Question>(
            @"SELECT id, test_id AS TestId, question_text AS QuestionText, position
              FROM questions WHERE test_id = @TestId ORDER BY position",
            new { TestId = id })).ToList();

        var qIds = questions.Select(q => q.Id).ToArray();
        if (qIds.Length > 0)
        {
            var answers = await connection.QueryAsync<Answer>(
                @"SELECT id, question_id AS QuestionId, answer_text AS AnswerText,
                         is_correct AS IsCorrect, position
                  FROM answers WHERE question_id = ANY(@Ids) ORDER BY position",
                new { Ids = qIds });

            var lookup = answers.ToLookup(a => a.QuestionId);
            foreach (var q in questions)
                q.Answers = lookup[q.Id].ToList();
        }

        return new TestDetailDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            TimeLimitMinutes = test.TimeLimitMinutes,
            PassingScore = test.PassingScore,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            Questions = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Position = q.Position,
                AllowMultipleAnswers = q.Answers?.Count(a => a.IsCorrect) > 1,
                Answers = q.Answers?.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    Position = a.Position,
                    IsCorrect = includeCorrectAnswers ? a.IsCorrect : null
                }).ToList() ?? []
            }).ToList()
        };
    }

    public async Task<PagedResponse<TestDto>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _db.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tests");

        var sql = $@"
            SELECT {TestBaseColumns}
            {TestBaseFrom}
            ORDER BY t.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var tests = (await connection.QueryAsync<Test>(sql,
            new { Limit = pageSize, Offset = (page - 1) * pageSize })).ToList();

        var counts = await LoadQuestionsCounts(connection, tests);

        return new PagedResponse<TestDto>
        {
            Items = tests.Select(t => ToDto(t, counts.GetValueOrDefault(t.Id, 0))).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResponse<TestDto>> GetAvailableForUserAsync(int userId, int page, int pageSize)
    {
        using var connection = _db.CreateConnection();

        const string notPassedCondition = @"
            NOT EXISTS (
                SELECT 1 FROM test_results tr
                WHERE tr.test_id = t.id AND tr.user_id = @UserId
                  AND tr.finished_at IS NOT NULL AND tr.score >= t.passing_score
            )";

        var countSql = $"SELECT COUNT(*) FROM tests t WHERE {notPassedCondition}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId });

        var sql = $@"
            SELECT {TestBaseColumns}
            {TestBaseFrom}
            WHERE {notPassedCondition}
            ORDER BY t.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var tests = (await connection.QueryAsync<Test>(sql,
            new { UserId = userId, Limit = pageSize, Offset = (page - 1) * pageSize })).ToList();

        var counts = await LoadQuestionsCounts(connection, tests);

        return new PagedResponse<TestDto>
        {
            Items = tests.Select(t => ToDto(t, counts.GetValueOrDefault(t.Id, 0))).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TestDto>> GetByLectureIdAsync(int lectureId)
    {
        using var connection = _db.CreateConnection();
        var sql = $@"
            SELECT {TestBaseColumns}
            {TestBaseFrom}
            WHERE t.lecture_id = @LectureId
            ORDER BY t.created_at DESC";

        var tests = (await connection.QueryAsync<Test>(sql, new { LectureId = lectureId })).ToList();
        var counts = await LoadQuestionsCounts(connection, tests);
        return tests.Select(t => ToDto(t, counts.GetValueOrDefault(t.Id, 0)));
    }

    public async Task<TestDto?> CreateAsync(CreateTestRequest request, int createdBy)
    {
        using var connection = _db.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var testId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO tests (lecture_id, title, description, time_limit_minutes, passing_score, created_by)
                  VALUES (@LectureId, @Title, @Description, @TimeLimitMinutes, @PassingScore, @CreatedBy)
                  RETURNING id",
                new
                {
                    request.LectureId,
                    request.Title,
                    request.Description,
                    request.TimeLimitMinutes,
                    PassingScore = request.PassingScore ?? 70,
                    CreatedBy = createdBy
                },
                transaction);

            foreach (var q in request.Questions)
            {
                var questionId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO questions (test_id, question_text, position)
                      VALUES (@TestId, @QuestionText, @Position) RETURNING id",
                    new { TestId = testId, q.QuestionText, q.Position }, transaction);

                foreach (var a in q.Answers)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO answers (question_id, answer_text, is_correct, position)
                          VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position)",
                        new { QuestionId = questionId, a.AnswerText, a.IsCorrect, a.Position }, transaction);
                }
            }

            transaction.Commit();
            return await GetByIdAsync(testId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateTestRequest request)
    {
        using var connection = _db.CreateConnection();
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tests WHERE id = @Id)", new { Id = id });
        if (!exists) return false;

        var sets = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        if (request.LectureId.HasValue) { sets.Add("lecture_id = @LectureId"); parameters.Add("LectureId", request.LectureId.Value); }
        if (request.Title != null) { sets.Add("title = @Title"); parameters.Add("Title", request.Title); }
        if (request.Description != null) { sets.Add("description = @Description"); parameters.Add("Description", request.Description); }
        if (request.TimeLimitMinutes.HasValue) { sets.Add("time_limit_minutes = @TimeLimitMinutes"); parameters.Add("TimeLimitMinutes", request.TimeLimitMinutes.Value); }
        if (request.PassingScore.HasValue) { sets.Add("passing_score = @PassingScore"); parameters.Add("PassingScore", request.PassingScore.Value); }

        if (sets.Count == 0) return true;

        var sql = $"UPDATE tests SET {string.Join(", ", sets)} WHERE id = @Id";
        return await connection.ExecuteAsync(sql, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM tests WHERE id = @Id", new { Id = id }) > 0;
    }

    public async Task<QuestionDto?> AddQuestionAsync(int testId, CreateQuestionRequest request)
    {
        using var connection = _db.CreateConnection();

        var testExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tests WHERE id = @Id)", new { Id = testId });
        if (!testExists) return null;

        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var questionId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO questions (test_id, question_text, position)
                  VALUES (@TestId, @QuestionText, @Position) RETURNING id",
                new { TestId = testId, request.QuestionText, request.Position }, transaction);

            foreach (var a in request.Answers)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO answers (question_id, answer_text, is_correct, position)
                      VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position)",
                    new { QuestionId = questionId, a.AnswerText, a.IsCorrect, a.Position }, transaction);
            }

            transaction.Commit();

            var question = await connection.QueryFirstOrDefaultAsync<Question>(
                @"SELECT id, test_id AS TestId, question_text AS QuestionText, position
                  FROM questions WHERE id = @Id", new { Id = questionId });

            var answers = await connection.QueryAsync<Answer>(
                @"SELECT id, question_id AS QuestionId, answer_text AS AnswerText,
                         is_correct AS IsCorrect, position
                  FROM answers WHERE question_id = @QuestionId ORDER BY position",
                new { QuestionId = questionId });

            return new QuestionDto
            {
                Id = question!.Id,
                QuestionText = question.QuestionText,
                Position = question.Position,
                AllowMultipleAnswers = answers.Count(a => a.IsCorrect) > 1,
                Answers = answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    Position = a.Position
                }).ToList()
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionRequest request)
    {
        using var connection = _db.CreateConnection();
        var question = await connection.QueryFirstOrDefaultAsync<Question>(
            @"SELECT id, test_id AS TestId, question_text AS QuestionText, position
              FROM questions WHERE id = @Id", new { Id = questionId });
        if (question == null) return false;

        if (request.QuestionText != null) question.QuestionText = request.QuestionText;
        if (request.Position.HasValue) question.Position = request.Position.Value;

        return await connection.ExecuteAsync(
            @"UPDATE questions SET question_text = @QuestionText, position = @Position WHERE id = @Id",
            question) > 0;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM questions WHERE id = @Id", new { Id = questionId }) > 0;
    }

    public async Task<AnswerDto?> AddAnswerAsync(int questionId, CreateAnswerRequest request)
    {
        using var connection = _db.CreateConnection();
        var questionExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM questions WHERE id = @Id)", new { Id = questionId });
        if (!questionExists) return null;

        var answerId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO answers (question_id, answer_text, is_correct, position)
              VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position) RETURNING id",
            new { QuestionId = questionId, request.AnswerText, request.IsCorrect, request.Position });

        return new AnswerDto
        {
            Id = answerId,
            AnswerText = request.AnswerText,
            IsCorrect = request.IsCorrect,
            Position = request.Position
        };
    }

    public async Task<bool> UpdateAnswerAsync(int answerId, UpdateAnswerRequest request)
    {
        using var connection = _db.CreateConnection();
        var answer = await connection.QueryFirstOrDefaultAsync<Answer>(
            @"SELECT id, question_id AS QuestionId, answer_text AS AnswerText,
                     is_correct AS IsCorrect, position
              FROM answers WHERE id = @Id", new { Id = answerId });
        if (answer == null) return false;

        if (request.AnswerText != null) answer.AnswerText = request.AnswerText;
        if (request.IsCorrect.HasValue) answer.IsCorrect = request.IsCorrect.Value;
        if (request.Position.HasValue) answer.Position = request.Position.Value;

        return await connection.ExecuteAsync(
            @"UPDATE answers SET answer_text = @AnswerText, is_correct = @IsCorrect, position = @Position WHERE id = @Id",
            answer) > 0;
    }

    public async Task<bool> DeleteAnswerAsync(int answerId)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM answers WHERE id = @Id", new { Id = answerId }) > 0;
    }

    public async Task<TestDto?> ImportFromPdfAsync(int lectureId, string title, string? description, int? timeLimitMinutes, Stream pdfStream, int createdBy)
    {
        var parsedData = await _pdfParserService.ParseTestFromPdfAsync(pdfStream);

        if (parsedData.Questions.Count == 0)
            throw new InvalidOperationException("PDF не содержит вопросов");

        using var connection = _db.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var testId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO tests (lecture_id, title, description, time_limit_minutes, created_by)
                  VALUES (@LectureId, @Title, @Description, @TimeLimitMinutes, @CreatedBy) RETURNING id",
                new { LectureId = lectureId, Title = title, Description = description, TimeLimitMinutes = timeLimitMinutes, CreatedBy = createdBy },
                transaction);

            int pos = 1;
            foreach (var pq in parsedData.Questions)
            {
                var questionId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO questions (test_id, question_text, position)
                      VALUES (@TestId, @QuestionText, @Position) RETURNING id",
                    new { TestId = testId, QuestionText = pq.Text, Position = pos++ }, transaction);

                int aPos = 1;
                foreach (var pa in pq.Answers)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO answers (question_id, answer_text, is_correct, position)
                          VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position)",
                        new { QuestionId = questionId, AnswerText = pa.Text, pa.IsCorrect, Position = aPos++ },
                        transaction);
                }
            }

            transaction.Commit();
            return await GetByIdAsync(testId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static TestDto ToDto(Test test, int questionsCount) => new()
    {
        Id = test.Id,
        LectureId = test.LectureId,
        LectureTitle = test.LectureTitle,
        Title = test.Title,
        Description = test.Description,
        TimeLimitMinutes = test.TimeLimitMinutes,
        PassingScore = test.PassingScore,
        CreatorUsername = test.CreatorUsername ?? "unknown",
        CreatedAt = test.CreatedAt,
        QuestionsCount = questionsCount
    };

    private static async Task<Dictionary<int, int>> LoadQuestionsCounts(System.Data.IDbConnection connection, IEnumerable<Test> tests)
    {
        var testIds = tests.Select(t => t.Id).ToArray();
        if (testIds.Length == 0) return new Dictionary<int, int>();

        var counts = await connection.QueryAsync<(int TestId, int Count)>(
            @"SELECT test_id AS TestId, COUNT(*) AS Count
              FROM questions WHERE test_id = ANY(@TestIds)
              GROUP BY test_id",
            new { TestIds = testIds });
        return counts.ToDictionary(r => r.TestId, r => r.Count);
    }
}
