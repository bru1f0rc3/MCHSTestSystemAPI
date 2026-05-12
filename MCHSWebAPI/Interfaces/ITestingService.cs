using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Interfaces;

public interface ITestingService
{
    Task<StartTestResponse?> StartTestAsync(int testId, int userId);
    Task<bool> SubmitAnswerAsync(int testResultId, int userId, SubmitAnswerRequest request);
    Task<bool> SubmitAnswersAsync(int testResultId, int userId, SubmitAnswersRequest request);
    Task<FinishTestResponse?> FinishTestAsync(int testResultId, int userId);
    Task<TestResultDto?> GetTestResultAsync(int testResultId, int userId);
    Task<TestResultDetailDto?> GetTestResultDetailAsync(int testResultId, int userId);
    Task<PagedResponse<TestResultDto>> GetUserResultsAsync(int userId, int page, int pageSize);
    Task<PagedResponse<TestResultDto>> GetAllResultsAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? searchQuery = null);
    Task<StartTestResponse?> GetInProgressTestAsync(int testId, int userId);
    Task<bool> RegisterCheatAttemptAsync(int testResultId, int userId, ReportCheatAttemptRequest request);
}
