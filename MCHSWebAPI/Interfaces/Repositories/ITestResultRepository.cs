using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface ITestResultRepository
{
    Task<TestResult?> GetByIdAsync(int id);
    Task<TestResult?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<TestResult>> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<IEnumerable<TestResult>> GetByTestIdAsync(int testId, int page, int pageSize);
    Task<IEnumerable<TestResult>> GetAllAsync(int page, int pageSize);
    Task<TestResult?> GetInProgressByUserAndTestAsync(int userId, int testId);
    Task<int> GetTotalCountAsync();
    Task<int> GetCountByUserIdAsync(int userId);
    Task<int> CreateAsync(TestResult testResult);
    Task<bool> UpdateAsync(TestResult testResult);
    Task<bool> FinishTestAsync(int id, double score);
    Task<bool> DeleteAsync(int id);
}
