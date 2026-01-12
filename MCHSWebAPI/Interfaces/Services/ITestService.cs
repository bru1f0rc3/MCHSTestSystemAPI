using MCHSWebAPI.DTOs.Tests;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Interfaces.Services;

public interface ITestService
{
    Task<TestDto?> GetByIdAsync(int id);
    Task<TestDetailDto?> GetByIdWithQuestionsAsync(int id, bool includeCorrectAnswers);
    Task<PagedResponse<TestDto>> GetAllAsync(int page, int pageSize);
    Task<PagedResponse<TestDto>> GetAvailableForUserAsync(int userId, int page, int pageSize);
    Task<IEnumerable<TestDto>> GetByLectureIdAsync(int lectureId);
    Task<TestDto?> CreateAsync(CreateTestRequest request, int createdBy);
    Task<bool> UpdateAsync(int id, UpdateTestRequest request);
    Task<bool> DeleteAsync(int id);

    // Questions
    Task<QuestionDto?> AddQuestionAsync(int testId, CreateQuestionRequest request);
    Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionRequest request);
    Task<bool> DeleteQuestionAsync(int questionId);

    // PDF Import
    Task<TestDto?> ImportFromPdfAsync(int lectureId, string title, string? description, Stream pdfStream, int createdBy);

    // Answers
    Task<AnswerWithCorrectDto?> AddAnswerAsync(int questionId, CreateAnswerRequest request);
    Task<bool> UpdateAnswerAsync(int answerId, UpdateAnswerRequest request);
    Task<bool> DeleteAnswerAsync(int answerId);
}
