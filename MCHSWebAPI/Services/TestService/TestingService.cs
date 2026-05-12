using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.TestService;

public class TestingService(IDbConnectionFactory db) : ITestingService
{
    private const string ResultSelectSql = @"
        SELECT tr.id, tr.user_id AS UserId, tr.test_id AS TestId, tr.started_at AS StartedAt,
               tr.finished_at AS FinishedAt, tr.score,
               tr.cheat_attempts AS CheatAttempts, tr.auto_submitted AS AutoSubmitted,
               u.username AS Username, t.title AS TestTitle, l.title AS LectureTitle,
               t.passing_score AS PassingScore, t.time_limit_minutes AS TimeLimitMinutes,
               CASE WHEN tr.finished_at IS NULL THEN 'in_progress'
                    WHEN tr.score >= t.passing_score THEN 'passed' ELSE 'failed' END AS Status
        FROM test_results tr
        JOIN users u ON tr.user_id = u.id
        JOIN tests t ON tr.test_id = t.id
        LEFT JOIN lectures l ON t.lecture_id = l.id";

    public async Task<StartTestResponse?> StartTestAsync(int testId, int userId)
    {
        var existing = await GetInProgressResult(userId, testId);
        if (existing != null)
        {
            var current = await GetInProgressTestAsync(testId, userId);
            if (current != null) return current;
        }

        var test = await GetTestWithQuestions(testId);
        if (test == null) return null;

        using var connection = db.CreateConnection();
        var startedAt = DateTime.UtcNow;
        var testResultId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO test_results (user_id, test_id, started_at)
              VALUES (@UserId, @TestId, @StartedAt) RETURNING id",
            new { UserId = userId, TestId = testId, StartedAt = startedAt });

        return BuildStartResponse(testResultId, test, startedAt);
    }

    public async Task<StartTestResponse?> GetInProgressTestAsync(int testId, int userId)
    {
        var result = await GetInProgressResult(userId, testId);
        if (result == null) return null;

        var test = await GetTestWithQuestions(testId);
        if (test == null) return null;
        if (IsTimedOut(result.StartedAt, test.TimeLimitMinutes))
        {
            await CalculateAndStoreScore(result.Id, autoSubmitted: true);
            return null;
        }

        return BuildStartResponse(result.Id, test, result.StartedAt);
    }

    public async Task<bool> SubmitAnswerAsync(int testResultId, int userId, SubmitAnswerRequest request)
    {
        using var connection = db.CreateConnection();

        var testResult = await LoadResultForSubmit(connection, testResultId);
        if (testResult == null || testResult.UserId != userId || testResult.FinishedAt.HasValue)
            return false;
        if (IsTimedOut(testResult.StartedAt, testResult.TimeLimitMinutes))
        {
            await CalculateAndStoreScore(testResultId, autoSubmitted: true);
            return false;
        }

        var question = await connection.QueryFirstOrDefaultAsync<Question>(
            "SELECT id, test_id AS TestId FROM questions WHERE id = @Id",
            new { Id = request.QuestionId });
        if (question == null || question.TestId != testResult.TestId)
            return false;

        var answer = await connection.QueryFirstOrDefaultAsync<Answer>(
            "SELECT id, question_id AS QuestionId FROM answers WHERE id = @Id",
            new { Id = request.AnswerId });
        if (answer == null || answer.QuestionId != request.QuestionId)
            return false;

        var exists = await connection.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(SELECT 1 FROM user_answers
              WHERE test_result_id = @TestResultId AND question_id = @QuestionId AND answer_id = @AnswerId)",
            new { TestResultId = testResultId, QuestionId = request.QuestionId, AnswerId = request.AnswerId });

        if (!exists)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO user_answers (test_result_id, question_id, answer_id, answered_at)
                  VALUES (@TestResultId, @QuestionId, @AnswerId, @AnsweredAt)",
                new { TestResultId = testResultId, QuestionId = request.QuestionId,
                      AnswerId = request.AnswerId, AnsweredAt = DateTime.UtcNow });
        }

        return true;
    }

    public async Task<bool> SubmitAnswersAsync(int testResultId, int userId, SubmitAnswersRequest request)
    {
        using var connection = db.CreateConnection();

        var testResult = await LoadResultForSubmit(connection, testResultId);
        if (testResult == null || testResult.UserId != userId || testResult.FinishedAt.HasValue)
            return false;
        if (IsTimedOut(testResult.StartedAt, testResult.TimeLimitMinutes))
        {
            await CalculateAndStoreScore(testResultId, autoSubmitted: true);
            return false;
        }

        var validAnswers = (await connection.QueryAsync<(int QuestionId, int AnswerId)>(
            @"SELECT q.id AS QuestionId, a.id AS AnswerId
              FROM questions q JOIN answers a ON a.question_id = q.id
              WHERE q.test_id = @TestId",
            new { testResult.TestId })).ToLookup(x => x.QuestionId, x => x.AnswerId);

        foreach (var group in request.Answers.GroupBy(a => a.QuestionId))
        {
            var questionId = group.Key;
            if (!validAnswers.Contains(questionId))
                return false;

            var validIds = validAnswers[questionId].ToHashSet();
            if (group.Any(a => !validIds.Contains(a.AnswerId)))
                return false;

            await connection.ExecuteAsync(
                "DELETE FROM user_answers WHERE test_result_id = @TestResultId AND question_id = @QuestionId",
                new { TestResultId = testResultId, QuestionId = questionId });

            foreach (var a in group)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO user_answers (test_result_id, question_id, answer_id, answered_at)
                      VALUES (@TestResultId, @QuestionId, @AnswerId, @AnsweredAt)",
                    new { TestResultId = testResultId, QuestionId = questionId,
                          AnswerId = a.AnswerId, AnsweredAt = DateTime.UtcNow });
            }
        }

        return true;
    }

    public async Task<FinishTestResponse?> FinishTestAsync(int testResultId, int userId)
    {
        var testResult = await GetTestResultWithDetails(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        if (!testResult.FinishedAt.HasValue)
        {
            await CalculateAndStoreScore(testResultId, autoSubmitted: false);
            testResult = await GetTestResultWithDetails(testResultId);
            if (testResult == null) return null;
        }

        return await BuildFinishResponse(testResultId, testResult);
    }

    public async Task<TestResultDto?> GetTestResultAsync(int testResultId, int userId)
    {
        var testResult = await GetTestResultWithDetails(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        return MapToDto(testResult);
    }

    public async Task<TestResultDetailDto?> GetTestResultDetailAsync(int testResultId, int userId)
    {
        var testResult = await GetTestResultWithDetails(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        using var connection = db.CreateConnection();
        var userAnswers = await GetUserAnswersForResult(connection, testResultId);
        var questionIds = userAnswers.Select(ua => ua.QuestionId).Distinct().ToList();
        var correctAnswersMap = await GetCorrectAnswersMap(questionIds);
        var (_, questionResults) = EvaluateQuestionResults(userAnswers, correctAnswersMap);

        return new TestResultDetailDto
        {
            Id = testResult.Id,
            TestId = testResult.TestId,
            TestTitle = testResult.TestTitle ?? "",
            LectureTitle = testResult.LectureTitle,
            StartedAt = testResult.StartedAt,
            FinishedAt = testResult.FinishedAt,
            Score = testResult.Score,
            PassingScore = testResult.PassingScore ?? 70,
            TimeLimitMinutes = testResult.TimeLimitMinutes,
            CheatAttempts = testResult.CheatAttempts,
            AutoSubmitted = testResult.AutoSubmitted,
            Status = testResult.Status ?? "unknown",
            Username = testResult.Username,
            QuestionResults = questionResults
        };
    }

    public async Task<PagedResponse<TestResultDto>> GetUserResultsAsync(int userId, int page, int pageSize)
    {
        using var connection = db.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM test_results WHERE user_id = @UserId",
            new { UserId = userId });

        var results = await connection.QueryAsync<TestResult>(
            $@"{ResultSelectSql}
               WHERE tr.user_id = @UserId
               ORDER BY tr.started_at DESC
               LIMIT @PageSize OFFSET @Offset",
            new { UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return new PagedResponse<TestResultDto>
        {
            Items = results.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResponse<TestResultDto>> GetAllResultsAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? searchQuery = null)
    {
        using var connection = db.CreateConnection();

        var (whereClause, parameters) = BuildAllResultsFilter(startDate, endDate, searchQuery);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            $@"SELECT COUNT(*)
               FROM test_results tr
               JOIN users u ON tr.user_id = u.id
               JOIN tests t ON tr.test_id = t.id
               WHERE {whereClause}",
            parameters);

        var results = await connection.QueryAsync<TestResult>(
            $@"{ResultSelectSql}
               WHERE {whereClause}
               ORDER BY tr.started_at DESC
               LIMIT @PageSize OFFSET @Offset",
            parameters);

        return new PagedResponse<TestResultDto>
        {
            Items = results.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> RegisterCheatAttemptAsync(int testResultId, int userId, ReportCheatAttemptRequest request)
    {
        using var connection = db.CreateConnection();
        var testResult = await connection.QueryFirstOrDefaultAsync<TestResult>(
            "SELECT id, user_id AS UserId, finished_at AS FinishedAt FROM test_results WHERE id = @Id",
            new { Id = testResultId });

        if (testResult == null || testResult.UserId != userId || testResult.FinishedAt.HasValue)
            return false;

        connection.Open();
        using var tx = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "UPDATE test_results SET cheat_attempts = cheat_attempts + 1 WHERE id = @Id",
                new { Id = testResultId }, tx);

            await connection.ExecuteAsync(
                @"INSERT INTO cheat_events (test_result_id, event_type, details)
                  VALUES (@TestResultId, @EventType, @Details)",
                new
                {
                    TestResultId = testResultId,
                    EventType = string.IsNullOrWhiteSpace(request.EventType) ? "app_background" : request.EventType,
                    request.Details
                }, tx);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }


    private static bool IsTimedOut(DateTime startedAt, int? timeLimitMinutes)
    {
        if (!timeLimitMinutes.HasValue || timeLimitMinutes.Value <= 0) return false;
        return DateTime.UtcNow > startedAt.AddMinutes(timeLimitMinutes.Value).AddSeconds(3);
    }

    private async Task CalculateAndStoreScore(int testResultId, bool autoSubmitted)
    {
        using var connection = db.CreateConnection();

        var testResult = await connection.QueryFirstOrDefaultAsync<TestResult>(
            @"SELECT id, test_id AS TestId, finished_at AS FinishedAt
              FROM test_results WHERE id = @Id",
            new { Id = testResultId });
        if (testResult == null || testResult.FinishedAt.HasValue) return;

        var totalQuestions = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM questions WHERE test_id = @TestId",
            new { testResult.TestId });

        var userAnswers = await GetUserAnswersForResult(connection, testResultId);
        var questionIds = userAnswers.Select(ua => ua.QuestionId).Distinct().ToList();
        var correctAnswersMap = await GetCorrectAnswersMap(questionIds);
        var (correctCount, _) = EvaluateQuestionResults(userAnswers, correctAnswersMap);

        var score = totalQuestions > 0
            ? Math.Round((double)correctCount / totalQuestions * 100, 2)
            : 0;

        await connection.ExecuteAsync(
            @"UPDATE test_results
              SET finished_at = @FinishedAt, score = @Score, auto_submitted = @AutoSubmitted
              WHERE id = @Id",
            new { Id = testResultId, FinishedAt = DateTime.UtcNow, Score = score, AutoSubmitted = autoSubmitted });
    }

    private async Task<TestResult?> LoadResultForSubmit(System.Data.IDbConnection connection, int testResultId)
    {
        return await connection.QueryFirstOrDefaultAsync<TestResult>(
            @"SELECT tr.id, tr.user_id AS UserId, tr.test_id AS TestId,
                     tr.started_at AS StartedAt, tr.finished_at AS FinishedAt,
                     t.time_limit_minutes AS TimeLimitMinutes
              FROM test_results tr
              JOIN tests t ON tr.test_id = t.id
              WHERE tr.id = @Id",
            new { Id = testResultId });
    }

    private async Task<TestResult?> GetInProgressResult(int userId, int testId)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TestResult>(
            @"SELECT tr.id, tr.user_id AS UserId, tr.test_id AS TestId,
                     tr.started_at AS StartedAt, tr.finished_at AS FinishedAt, tr.score,
                     t.time_limit_minutes AS TimeLimitMinutes
              FROM test_results tr
              JOIN tests t ON tr.test_id = t.id
              WHERE tr.user_id = @UserId AND tr.test_id = @TestId AND tr.finished_at IS NULL
              ORDER BY tr.started_at DESC LIMIT 1",
            new { UserId = userId, TestId = testId });
    }

    private async Task<Test?> GetTestWithQuestions(int testId)
    {
        using var connection = db.CreateConnection();

        var test = await connection.QueryFirstOrDefaultAsync<Test>(
            @"SELECT t.id, t.lecture_id AS LectureId, t.title, t.description,
                     t.time_limit_minutes AS TimeLimitMinutes, t.passing_score AS PassingScore,
                     t.created_by AS CreatedBy, t.created_at AS CreatedAt,
                     l.title AS LectureTitle, u.username AS CreatorUsername
              FROM tests t
              LEFT JOIN lectures l ON t.lecture_id = l.id
              JOIN users u ON t.created_by = u.id
              WHERE t.id = @Id",
            new { Id = testId });
        if (test == null) return null;

        var questions = (await connection.QueryAsync<Question>(
            @"SELECT id, test_id AS TestId, question_text AS QuestionText, position
              FROM questions WHERE test_id = @TestId ORDER BY position",
            new { TestId = testId })).ToList();

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

        test.Questions = questions;
        return test;
    }

    private async Task<TestResult?> GetTestResultWithDetails(int testResultId)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TestResult>(
            $"{ResultSelectSql} WHERE tr.id = @Id",
            new { Id = testResultId });
    }

    private async Task<Dictionary<int, List<Answer>>> GetCorrectAnswersMap(List<int> questionIds)
    {
        if (questionIds.Count == 0) return new Dictionary<int, List<Answer>>();

        using var connection = db.CreateConnection();
        var answers = await connection.QueryAsync<Answer>(
            @"SELECT id, question_id AS QuestionId, answer_text AS AnswerText,
                     is_correct AS IsCorrect, position
              FROM answers WHERE question_id = ANY(@Ids) AND is_correct = true",
            new { Ids = questionIds.ToArray() });

        return answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static string FormatCorrectAnswers(Dictionary<int, List<Answer>> map, int questionId)
    {
        if (!map.TryGetValue(questionId, out var answers) || answers.Count == 0)
            return "";
        return string.Join(", ", answers.Select(a => a.AnswerText));
    }

    private async Task<List<UserAnswer>> GetUserAnswersForResult(System.Data.IDbConnection connection, int testResultId)
    {
        return (await connection.QueryAsync<UserAnswer>(
            @"SELECT ua.id, ua.test_result_id AS TestResultId, ua.question_id AS QuestionId,
                     ua.answer_id AS AnswerId, ua.answered_at AS AnsweredAt,
                     q.question_text AS QuestionText, a.answer_text AS AnswerText, a.is_correct AS IsCorrect
              FROM user_answers ua
              JOIN questions q ON ua.question_id = q.id
              LEFT JOIN answers a ON ua.answer_id = a.id
              WHERE ua.test_result_id = @TestResultId ORDER BY q.position",
            new { TestResultId = testResultId })).ToList();
    }

    private static (int CorrectCount, List<QuestionResultDto> Results) EvaluateQuestionResults(
        List<UserAnswer> userAnswers,
        Dictionary<int, List<Answer>> correctAnswersMap)
    {
        var results = new List<QuestionResultDto>();
        int correctCount = 0;

        foreach (var group in userAnswers.GroupBy(ua => ua.QuestionId))
        {
            var qId = group.Key;
            var correctIds = correctAnswersMap.TryGetValue(qId, out var list)
                ? list.Select(a => a.Id).ToHashSet()
                : new HashSet<int>();
            var userIds = group.Select(ua => ua.AnswerId ?? 0).Where(id => id > 0).ToHashSet();
            var isCorrect = correctIds.SetEquals(userIds);
            if (isCorrect) correctCount++;

            results.Add(new QuestionResultDto
            {
                QuestionId = qId,
                QuestionText = group.First().QuestionText ?? "",
                UserAnswer = string.Join(", ", group.Select(ua => ua.AnswerText).Where(t => t != null)),
                CorrectAnswer = FormatCorrectAnswers(correctAnswersMap, qId),
                IsCorrect = isCorrect
            });
        }

        return (correctCount, results);
    }

    private async Task<FinishTestResponse> BuildFinishResponse(int testResultId, TestResult testResult)
    {
        using var connection = db.CreateConnection();

        var totalQuestions = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM questions WHERE test_id = @TestId",
            new { testResult.TestId });

        var userAnswers = await GetUserAnswersForResult(connection, testResultId);
        var questionIds = userAnswers.Select(ua => ua.QuestionId).Distinct().ToList();
        var correctAnswersMap = await GetCorrectAnswersMap(questionIds);
        var passingScore = testResult.PassingScore ?? 70;
        var (correctCount, questionResults) = EvaluateQuestionResults(userAnswers, correctAnswersMap);

        return new FinishTestResponse
        {
            TestResultId = testResultId,
            TestTitle = testResult.TestTitle ?? "",
            StartedAt = testResult.StartedAt,
            FinishedAt = testResult.FinishedAt ?? DateTime.UtcNow,
            Score = testResult.Score ?? 0,
            PassingScore = passingScore,
            Status = (testResult.Score ?? 0) >= passingScore ? "passed" : "failed",
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctCount,
            CheatAttempts = testResult.CheatAttempts,
            AutoSubmitted = testResult.AutoSubmitted,
            QuestionResults = questionResults
        };
    }

    private static StartTestResponse BuildStartResponse(int testResultId, Test test, DateTime startedAt)
    {
        return new StartTestResponse
        {
            TestResultId = testResultId,
            TestId = test.Id,
            TestTitle = test.Title,
            StartedAt = startedAt,
            TimeLimitMinutes = test.TimeLimitMinutes,
            DeadlineAt = test.TimeLimitMinutes.HasValue ? startedAt.AddMinutes(test.TimeLimitMinutes.Value) : null,
            PassingScore = test.PassingScore,
            Questions = MapQuestions(test.Questions)
        };
    }

    private static List<TestQuestionDto> MapQuestions(IEnumerable<Question>? questions)
    {
        return questions?.Select(q => new TestQuestionDto
        {
            QuestionId = q.Id,
            QuestionText = q.QuestionText,
            Position = q.Position,
            AllowMultipleAnswers = q.Answers?.Count(a => a.IsCorrect) > 1,
            Answers = q.Answers?.Select(a => new TestAnswerDto
            {
                AnswerId = a.Id,
                AnswerText = a.AnswerText,
                Position = a.Position
            }).ToList() ?? []
        }).ToList() ?? [];
    }

    private static TestResultDto MapToDto(TestResult result) => new()
    {
        Id = result.Id,
        TestId = result.TestId,
        TestTitle = result.TestTitle ?? "",
        LectureTitle = result.LectureTitle,
        Username = result.Username,
        StartedAt = result.StartedAt,
        FinishedAt = result.FinishedAt,
        Score = result.Score,
        PassingScore = result.PassingScore ?? 70,
        TimeLimitMinutes = result.TimeLimitMinutes,
        CheatAttempts = result.CheatAttempts,
        AutoSubmitted = result.AutoSubmitted,
        Status = result.Status ?? "unknown"
    };

    private static (string Where, DynamicParameters Params) BuildAllResultsFilter(DateTime? startDate, DateTime? endDate, string? searchQuery)
    {
        var conditions = new List<string> { "1=1" };
        var parameters = new DynamicParameters();

        if (startDate.HasValue)
        {
            conditions.Add("tr.started_at >= @StartDate");
            parameters.Add("StartDate", startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            conditions.Add("tr.started_at <= @EndDate");
            parameters.Add("EndDate", endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            conditions.Add("(u.username ILIKE @SearchQuery OR t.title ILIKE @SearchQuery)");
            parameters.Add("SearchQuery", $"%{searchQuery}%");
        }

        return (string.Join(" AND ", conditions), parameters);
    }

}
