using MCHSWebAPI.Models;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Testing;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Services;

// Сервис для прохождения тестов
public class TestingService : ITestingService
{
    private readonly ITestRepository _testRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly ITestResultRepository _testResultRepository;
    private readonly IUserAnswerRepository _userAnswerRepository;

    public TestingService(
        ITestRepository testRepository,
        IQuestionRepository questionRepository,
        IAnswerRepository answerRepository,
        ITestResultRepository testResultRepository,
        IUserAnswerRepository userAnswerRepository)
    {
        _testRepository = testRepository;
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _testResultRepository = testResultRepository;
        _userAnswerRepository = userAnswerRepository;
    }

    public async Task<StartTestResponse?> StartTestAsync(int testId, int userId)
    {
        // Проверяем незавершенный тест
        var existing = await _testResultRepository.GetInProgressByUserAndTestAsync(userId, testId);
        if (existing != null)
            return await GetInProgressTestAsync(testId, userId);

        var test = await _testRepository.GetByIdWithQuestionsAsync(testId);
        if (test == null) return null;

        // Создаем попытку
        var testResultId = await _testResultRepository.CreateAsync(new TestResult
        {
            UserId = userId,
            TestId = testId
        });

        return new StartTestResponse
        {
            TestResultId = testResultId,
            TestId = test.Id,
            TestTitle = test.Title,
            StartedAt = DateTime.UtcNow,
            Questions = MapQuestions(test.Questions)
        };
    }

    public async Task<StartTestResponse?> GetInProgressTestAsync(int testId, int userId)
    {
        var result = await _testResultRepository.GetInProgressByUserAndTestAsync(userId, testId);
        if (result == null) return null;

        var test = await _testRepository.GetByIdWithQuestionsAsync(testId);
        if (test == null) return null;

        return new StartTestResponse
        {
            TestResultId = result.Id,
            TestId = test.Id,
            TestTitle = test.Title,
            StartedAt = result.StartedAt,
            Questions = MapQuestions(test.Questions)
        };
    }

    public async Task<bool> SubmitAnswerAsync(int testResultId, int userId, SubmitAnswerRequest request)
    {
        var testResult = await _testResultRepository.GetByIdAsync(testResultId);
        if (testResult == null || testResult.UserId != userId || testResult.FinishedAt.HasValue)
            return false;

        // Проверки
        var question = await _questionRepository.GetByIdAsync(request.QuestionId);
        if (question == null || question.TestId != testResult.TestId)
            return false;

        var answer = await _answerRepository.GetByIdAsync(request.AnswerId);
        if (answer == null || answer.QuestionId != request.QuestionId)
            return false;

        // Обновляем или создаем ответ
        var existing = await _userAnswerRepository.GetByTestResultAndQuestionAsync(testResultId, request.QuestionId);
        if (existing != null)
        {
            existing.AnswerId = request.AnswerId;
            return await _userAnswerRepository.UpdateAsync(existing);
        }

        await _userAnswerRepository.CreateAsync(new UserAnswer
        {
            TestResultId = testResultId,
            QuestionId = request.QuestionId,
            AnswerId = request.AnswerId
        });
        return true;
    }

    public async Task<bool> SubmitAnswersAsync(int testResultId, int userId, SubmitAnswersRequest request)
    {
        foreach (var a in request.Answers)
        {
            if (!await SubmitAnswerAsync(testResultId, userId, a))
                return false;
        }
        return true;
    }

    public async Task<FinishTestResponse?> FinishTestAsync(int testResultId, int userId)
    {
        var testResult = await _testResultRepository.GetByIdWithDetailsAsync(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        if (testResult.FinishedAt.HasValue)
            return await BuildFinishResponse(testResultId, testResult);

        // Подсчет результата
        var totalQuestions = await _questionRepository.GetCountByTestIdAsync(testResult.TestId);
        var correctAnswers = await _userAnswerRepository.GetCorrectCountByTestResultIdAsync(testResultId);
        
        var score = totalQuestions > 0 
            ? Math.Round((double)correctAnswers / totalQuestions * 100, 2) 
            : 0;

        await _testResultRepository.FinishTestAsync(testResultId, score);
        testResult = await _testResultRepository.GetByIdWithDetailsAsync(testResultId);
        
        return testResult == null ? null : await BuildFinishResponse(testResultId, testResult);
    }

    private async Task<FinishTestResponse> BuildFinishResponse(int testResultId, TestResult testResult)
    {
        var totalQuestions = await _questionRepository.GetCountByTestIdAsync(testResult.TestId);
        var correctAnswers = await _userAnswerRepository.GetCorrectCountByTestResultIdAsync(testResultId);
        var userAnswers = (await _userAnswerRepository.GetByTestResultIdAsync(testResultId)).ToList();

        // Batch запрос для правильных ответов
        var questionIds = userAnswers.Select(ua => ua.QuestionId).ToList();
        var correctAnswersMap = await _answerRepository.GetCorrectAnswersByQuestionIdsAsync(questionIds);

        var questionResults = userAnswers.Select(ua => new QuestionResultDto
        {
            QuestionId = ua.QuestionId,
            QuestionText = ua.QuestionText ?? "",
            UserAnswer = ua.AnswerText,
            CorrectAnswer = correctAnswersMap.GetValueOrDefault(ua.QuestionId)?.AnswerText ?? "",
            IsCorrect = ua.IsCorrect ?? false
        }).ToList();

        return new FinishTestResponse
        {
            TestResultId = testResultId,
            TestTitle = testResult.TestTitle ?? "",
            StartedAt = testResult.StartedAt,
            FinishedAt = testResult.FinishedAt ?? DateTime.UtcNow,
            Score = testResult.Score ?? 0,
            Status = testResult.Score >= 70 ? "passed" : "failed",
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctAnswers,
            QuestionResults = questionResults
        };
    }

    public async Task<TestResultDto?> GetTestResultAsync(int testResultId, int userId)
    {
        var testResult = await _testResultRepository.GetByIdWithDetailsAsync(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        return MapToDto(testResult);
    }

    public async Task<TestResultDetailDto?> GetTestResultDetailAsync(int testResultId, int userId)
    {
        var testResult = await _testResultRepository.GetByIdWithDetailsAsync(testResultId);
        if (testResult == null || testResult.UserId != userId)
            return null;

        var userAnswers = (await _userAnswerRepository.GetByTestResultIdAsync(testResultId)).ToList();
        var questionIds = userAnswers.Select(ua => ua.QuestionId).ToList();
        var correctAnswersMap = await _answerRepository.GetCorrectAnswersByQuestionIdsAsync(questionIds);

        var questionResults = userAnswers.Select(ua => new QuestionResultDto
        {
            QuestionId = ua.QuestionId,
            QuestionText = ua.QuestionText ?? "",
            UserAnswer = ua.AnswerText,
            CorrectAnswer = correctAnswersMap.GetValueOrDefault(ua.QuestionId)?.AnswerText ?? "",
            IsCorrect = ua.IsCorrect ?? false
        }).ToList();

        return new TestResultDetailDto
        {
            Id = testResult.Id,
            TestId = testResult.TestId,
            TestTitle = testResult.TestTitle ?? "",
            StartedAt = testResult.StartedAt,
            FinishedAt = testResult.FinishedAt,
            Score = testResult.Score,
            Status = testResult.Status ?? "unknown",
            QuestionResults = questionResults
        };
    }

    public async Task<PagedResponse<TestResultDto>> GetUserResultsAsync(int userId, int page, int pageSize)
    {
        var results = await _testResultRepository.GetByUserIdAsync(userId, page, pageSize);
        var totalCount = await _testResultRepository.GetCountByUserIdAsync(userId);

        return new PagedResponse<TestResultDto>
        {
            Items = results.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResponse<TestResultDto>> GetAllResultsAsync(int page, int pageSize)
    {
        var results = await _testResultRepository.GetAllAsync(page, pageSize);
        var totalCount = await _testResultRepository.GetTotalCountAsync();

        return new PagedResponse<TestResultDto>
        {
            Items = results.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // Вспомогательные методы
    private static List<TestQuestionDto> MapQuestions(IEnumerable<Question>? questions)
    {
        return questions?.Select(q => new TestQuestionDto
        {
            QuestionId = q.Id,
            QuestionText = q.QuestionText,
            Position = q.Position,
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
        StartedAt = result.StartedAt,
        FinishedAt = result.FinishedAt,
        Score = result.Score,
        Status = result.Status ?? "unknown"
    };
}
