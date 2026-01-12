using MCHSWebAPI.Models;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Tests;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Services;

public class TestService : ITestService
{
    private readonly ITestRepository _testRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IPdfParserService _pdfParserService;

    public TestService(
        ITestRepository testRepository,
        IQuestionRepository questionRepository,
        IAnswerRepository answerRepository,
        IPdfParserService pdfParserService)
    {
        _testRepository = testRepository;
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _pdfParserService = pdfParserService;
    }

    public async Task<TestDto?> GetByIdAsync(int id)
    {
        var test = await _testRepository.GetByIdAsync(id);
        if (test == null) return null;

        var questionsCount = await _questionRepository.GetCountByTestIdAsync(id);

        return new TestDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            QuestionsCount = questionsCount
        };
    }

    public async Task<TestDetailDto?> GetByIdWithQuestionsAsync(int id, bool includeCorrectAnswers)
    {
        var test = await _testRepository.GetByIdWithQuestionsAsync(id);
        if (test == null) return null;

        return new TestDetailDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            Questions = test.Questions?.Select(q => new QuestionDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Position = q.Position,
                Answers = q.Answers?.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    Position = a.Position
                }).ToList() ?? []
            }).ToList() ?? []
        };
    }

    public async Task<PagedResponse<TestDto>> GetAllAsync(int page, int pageSize)
    {
        var tests = await _testRepository.GetAllAsync(page, pageSize);
        var totalCount = await _testRepository.GetTotalCountAsync();

        var testIds = tests.Select(t => t.Id).ToList();
        var questionsCounts = await _questionRepository.GetCountsByTestIdsAsync(testIds);

        var testDtos = tests.Select(test => new TestDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            QuestionsCount = questionsCounts.GetValueOrDefault(test.Id, 0)
        }).ToList();

        return new PagedResponse<TestDto>
        {
            Items = testDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResponse<TestDto>> GetAvailableForUserAsync(int userId, int page, int pageSize)
    {
        var tests = await _testRepository.GetAvailableForUserAsync(userId, page, pageSize);
        var totalCount = await _testRepository.GetAvailableCountForUserAsync(userId);

        var testIds = tests.Select(t => t.Id).ToList();
        var questionsCounts = await _questionRepository.GetCountsByTestIdsAsync(testIds);

        var testDtos = tests.Select(test => new TestDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            QuestionsCount = questionsCounts.GetValueOrDefault(test.Id, 0)
        }).ToList();

        return new PagedResponse<TestDto>
        {
            Items = testDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<TestDto>> GetByLectureIdAsync(int lectureId)
    {
        var tests = await _testRepository.GetByLectureIdAsync(lectureId);
        var testList = tests.ToList();

        var testIds = testList.Select(t => t.Id).ToList();
        var questionsCounts = await _questionRepository.GetCountsByTestIdsAsync(testIds);

        return testList.Select(test => new TestDto
        {
            Id = test.Id,
            LectureId = test.LectureId,
            LectureTitle = test.LectureTitle,
            Title = test.Title,
            Description = test.Description,
            CreatorUsername = test.CreatorUsername ?? "unknown",
            CreatedAt = test.CreatedAt,
            QuestionsCount = questionsCounts.GetValueOrDefault(test.Id, 0)
        });
    }

    public async Task<TestDto?> CreateAsync(CreateTestRequest request, int createdBy)
    {
        var test = new Test
        {
            LectureId = request.LectureId,
            Title = request.Title,
            Description = request.Description,
            CreatedBy = createdBy
        };

        var testId = await _testRepository.CreateAsync(test);

        foreach (var q in request.Questions)
        {
            var question = new Question
            {
                TestId = testId,
                QuestionText = q.QuestionText,
                Position = q.Position
            };
            var questionId = await _questionRepository.CreateAsync(question);

            foreach (var a in q.Answers)
            {
                await _answerRepository.CreateAsync(new Answer
                {
                    QuestionId = questionId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect,
                    Position = a.Position
                });
            }
        }

        return await GetByIdAsync(testId);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTestRequest request)
    {
        var test = await _testRepository.GetByIdAsync(id);
        if (test == null) return false;

        if (request.LectureId.HasValue) test.LectureId = request.LectureId;
        if (request.Title != null) test.Title = request.Title;
        if (request.Description != null) test.Description = request.Description;

        return await _testRepository.UpdateAsync(test);
    }

    public async Task<bool> DeleteAsync(int id) => await _testRepository.DeleteAsync(id);

    public async Task<QuestionDto?> AddQuestionAsync(int testId, CreateQuestionRequest request)
    {
        var test = await _testRepository.GetByIdAsync(testId);
        if (test == null) return null;

        var question = new Question
        {
            TestId = testId,
            QuestionText = request.QuestionText,
            Position = request.Position
        };
        var questionId = await _questionRepository.CreateAsync(question);

        foreach (var a in request.Answers)
        {
            await _answerRepository.CreateAsync(new Answer
            {
                QuestionId = questionId,
                AnswerText = a.AnswerText,
                IsCorrect = a.IsCorrect,
                Position = a.Position
            });
        }

        var created = await _questionRepository.GetByIdWithAnswersAsync(questionId);
        if (created == null) return null;

        return new QuestionDto
        {
            Id = created.Id,
            QuestionText = created.QuestionText,
            Position = created.Position,
            Answers = created.Answers?.Select(a => new AnswerDto
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                Position = a.Position
            }).ToList() ?? []
        };
    }

    public async Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionRequest request)
    {
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null) return false;

        if (request.QuestionText != null) question.QuestionText = request.QuestionText;
        if (request.Position.HasValue) question.Position = request.Position.Value;

        return await _questionRepository.UpdateAsync(question);
    }

    public async Task<bool> DeleteQuestionAsync(int questionId) => await _questionRepository.DeleteAsync(questionId);

    public async Task<AnswerWithCorrectDto?> AddAnswerAsync(int questionId, CreateAnswerRequest request)
    {
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null) return null;

        var answer = new Answer
        {
            QuestionId = questionId,
            AnswerText = request.AnswerText,
            IsCorrect = request.IsCorrect,
            Position = request.Position
        };
        var answerId = await _answerRepository.CreateAsync(answer);

        return new AnswerWithCorrectDto
        {
            Id = answerId,
            AnswerText = answer.AnswerText,
            IsCorrect = answer.IsCorrect,
            Position = answer.Position
        };
    }

    public async Task<bool> UpdateAnswerAsync(int answerId, UpdateAnswerRequest request)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId);
        if (answer == null) return false;

        if (request.AnswerText != null) answer.AnswerText = request.AnswerText;
        if (request.IsCorrect.HasValue) answer.IsCorrect = request.IsCorrect.Value;
        if (request.Position.HasValue) answer.Position = request.Position.Value;

        return await _answerRepository.UpdateAsync(answer);
    }

    public async Task<bool> DeleteAnswerAsync(int answerId) => await _answerRepository.DeleteAsync(answerId);

    public async Task<TestDto?> ImportFromPdfAsync(int lectureId, string title, string? description, Stream pdfStream, int createdBy)
    {
        var parsedData = await _pdfParserService.ParseTestFromPdfAsync(pdfStream);

        if (parsedData.Questions.Count == 0)
            throw new InvalidOperationException("PDF не содержит вопросов");

        var test = new Test
        {
            LectureId = lectureId,
            Title = title,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var testId = await _testRepository.CreateAsync(test);
        if (testId == 0)
            throw new InvalidOperationException("Не удалось создать тест");

        int pos = 1;
        foreach (var pq in parsedData.Questions)
        {
            var questionId = await _questionRepository.CreateAsync(new Question
            {
                TestId = testId,
                QuestionText = pq.Text,
                Position = pos++
            });

            if (questionId == 0) continue;

            int aPos = 1;
            foreach (var pa in pq.Answers)
            {
                await _answerRepository.CreateAsync(new Answer
                {
                    QuestionId = questionId,
                    AnswerText = pa.Text,
                    IsCorrect = pa.IsCorrect,
                    Position = aPos++
                });
            }
        }

        return await GetByIdAsync(testId);
    }
}
