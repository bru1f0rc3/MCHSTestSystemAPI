using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.TestService;

public class TestService(IDbConnectionFactory db, IPdfParserService pdfParserService) : ITestService
{
    private const string TestBaseColumns = @"
        t.id, t.lecture_id AS LectureId, t.title, t.description,
        t.time_limit_minutes AS TimeLimitMinutes, t.passing_score AS PassingScore,
        t.created_by AS CreatedBy, t.created_at AS CreatedAt,
        l.title AS LectureTitle, u.username AS CreatorUsername";

    private const string TestBaseFrom = @"
        FROM tests t
        LEFT JOIN lectures l ON t.lecture_id = l.id
        JOIN users u ON t.created_by = u.id";

    /// <summary>
    /// Находит один тест по его номеру и считает количество вопросов в нём
    /// </summary>
    /// <param name="id">Номер (id) теста, который нужно найти</param>
    public async Task<TestDto?> GetByIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        var sql = $"SELECT {TestBaseColumns} {TestBaseFrom} WHERE t.id = @Id";

        var test = await connection.QueryFirstOrDefaultAsync<Test>(sql, new { Id = id });
        if (test == null) return null;

        var questionsCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM questions WHERE test_id = @Id", new { Id = id });

        return ToDto(test, questionsCount);
    }

    /// <summary>
    /// Находит тест по номеру вместе со всеми его вопросами и вариантами ответов
    /// </summary>
    /// <param name="id">Номер (id) теста, который нужно найти</param>
    /// <param name="includeCorrectAnswers">Показывать ли, какие ответы правильные (true — да, false — скрыть)</param>
    public async Task<TestDetailDto?> GetByIdWithQuestionsAsync(int id, bool includeCorrectAnswers)
    {
        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Возвращает список всех тестов по страницам
    /// </summary>
    /// <param name="page">Номер страницы, которую нужно показать</param>
    /// <param name="pageSize">Сколько тестов помещается на одной странице</param>
    public async Task<PagedResponse<TestDto>> GetAllAsync(int page, int pageSize)
    {
        using var connection = db.CreateConnection();

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

    /// <summary>
    /// Возвращает тесты, которые пользователь ещё не сдал, по страницам
    /// </summary>
    /// <param name="userId">Номер пользователя, для которого ищем доступные тесты</param>
    /// <param name="page">Номер страницы, которую нужно показать</param>
    /// <param name="pageSize">Сколько тестов помещается на одной странице</param>
    public async Task<PagedResponse<TestDto>> GetAvailableForUserAsync(int userId, int page, int pageSize)
    {
        using var connection = db.CreateConnection();

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

    /// <summary>
    /// Возвращает все тесты, привязанные к одной лекции
    /// </summary>
    /// <param name="lectureId">Номер (id) лекции, тесты которой нужны</param>
    public async Task<IEnumerable<TestDto>> GetByLectureIdAsync(int lectureId)
    {
        using var connection = db.CreateConnection();
        var sql = $@"
            SELECT {TestBaseColumns}
            {TestBaseFrom}
            WHERE t.lecture_id = @LectureId
            ORDER BY t.created_at DESC";

        var tests = (await connection.QueryAsync<Test>(sql, new { LectureId = lectureId })).ToList();
        var counts = await LoadQuestionsCounts(connection, tests);
        return tests.Select(t => ToDto(t, counts.GetValueOrDefault(t.Id, 0)));
    }

    /// <summary>
    /// Создаёт новый тест вместе со всеми его вопросами и ответами
    /// </summary>
    /// <param name="request">Данные теста: название, описание, вопросы и ответы</param>
    /// <param name="createdBy">Номер пользователя, который создаёт тест</param>
    public async Task<TestDto?> CreateAsync(CreateTestRequest request, int createdBy)
    {
        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Обновляет тест: меняет только те поля, что переданы
    /// </summary>
    /// <param name="id">Номер (id) теста, который нужно изменить</param>
    /// <param name="request">Новые данные теста (поля, которые надо обновить)</param>
    public async Task<bool> UpdateAsync(int id, UpdateTestRequest request)
    {
        using var connection = db.CreateConnection();
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

        if (sets.Count == 0) return false;

        var sql = $"UPDATE tests SET {string.Join(", ", sets)} WHERE id = @Id";
        return await connection.ExecuteAsync(sql, parameters) > 0;
    }

    /// <summary>
    /// Удаляет тест по его номеру
    /// </summary>
    /// <param name="id">Номер (id) теста, который нужно удалить</param>
    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM tests WHERE id = @Id", new { Id = id }) > 0;
    }

    /// <summary>
    /// Добавляет в тест новый вопрос вместе с вариантами ответов
    /// </summary>
    /// <param name="testId">Номер (id) теста, в который добавляем вопрос</param>
    /// <param name="request">Данные вопроса: текст, позиция и список ответов</param>
    public async Task<QuestionDto?> AddQuestionAsync(int testId, CreateQuestionRequest request)
    {
        using var connection = db.CreateConnection();

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

    /// <summary>
    /// Обновляет текст или позицию вопроса
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса, который нужно изменить</param>
    /// <param name="request">Новые данные вопроса (поля, которые надо обновить)</param>
    public async Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionRequest request)
    {
        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Удаляет вопрос по его номеру
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса, который нужно удалить</param>
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM questions WHERE id = @Id", new { Id = questionId }) > 0;
    }

    /// <summary>
    /// Добавляет новый вариант ответа к вопросу
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса, к которому добавляем ответ</param>
    /// <param name="request">Данные ответа: текст, правильность и позиция</param>
    public async Task<AnswerDto?> AddAnswerAsync(int questionId, CreateAnswerRequest request)
    {
        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Обновляет вариант ответа: текст, правильность или позицию
    /// </summary>
    /// <param name="answerId">Номер (id) ответа, который нужно изменить</param>
    /// <param name="request">Новые данные ответа (поля, которые надо обновить)</param>
    public async Task<bool> UpdateAnswerAsync(int answerId, UpdateAnswerRequest request)
    {
        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Удаляет вариант ответа по его номеру
    /// </summary>
    /// <param name="answerId">Номер (id) ответа, который нужно удалить</param>
    public async Task<bool> DeleteAnswerAsync(int answerId)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM answers WHERE id = @Id", new { Id = answerId }) > 0;
    }

    /// <summary>
    /// Создаёт новый тест, прочитав вопросы и ответы из загруженного PDF-файла
    /// </summary>
    /// <param name="lectureId">Номер (id) лекции, к которой привязываем тест</param>
    /// <param name="title">Название будущего теста</param>
    /// <param name="description">Описание теста (можно не указывать)</param>
    /// <param name="timeLimitMinutes">Ограничение по времени в минутах (можно не указывать)</param>
    /// <param name="pdfStream">Поток с содержимым PDF-файла</param>
    /// <param name="createdBy">Номер пользователя, который создаёт тест</param>
    public async Task<TestDto?> ImportFromPdfAsync(int lectureId, string title, string? description, int? timeLimitMinutes, Stream pdfStream, int createdBy)
    {
        var parsedData = await pdfParserService.ParseTestFromPdfAsync(pdfStream);

        if (parsedData.Questions.Count == 0)
            throw new InvalidOperationException("PDF не содержит вопросов");

        using var connection = db.CreateConnection();
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

    /// <summary>
    /// Превращает тест из базы в объект для отправки клиенту (DTO)
    /// </summary>
    /// <param name="test">Тест, прочитанный из базы данных</param>
    /// <param name="questionsCount">Сколько вопросов в этом тесте</param>
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

    /// <summary>
    /// Считает количество вопросов сразу для нескольких тестов
    /// (возвращает: номер теста — число вопросов)
    /// </summary>
    /// <param name="connection">Открытое подключение к базе данных</param>
    /// <param name="tests">Список тестов, для которых нужно посчитать вопросы</param>
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
