using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Services.TestService.TestService;
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
    Task<QuestionDto?> AddQuestionAsync(int testId, CreateQuestionRequest request);
    Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionRequest request);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<AnswerDto?> AddAnswerAsync(int questionId, CreateAnswerRequest request);
    Task<bool> UpdateAnswerAsync(int answerId, UpdateAnswerRequest request);
    Task<bool> DeleteAnswerAsync(int answerId);
    Task<TestDto?> ImportFromPdfAsync(int lectureId, string title, string? description, int? timeLimitMinutes, Stream pdfStream, int createdBy);
}
