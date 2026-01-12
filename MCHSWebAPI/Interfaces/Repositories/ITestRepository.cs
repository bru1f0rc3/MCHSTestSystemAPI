using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id);
    Task<Test?> GetByIdWithQuestionsAsync(int id);
    Task<IEnumerable<Test>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Test>> GetByLectureIdAsync(int lectureId);
    Task<IEnumerable<Test>> GetAvailableForUserAsync(int userId, int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> GetAvailableCountForUserAsync(int userId);
    Task<int> CreateAsync(Test test);
    Task<bool> UpdateAsync(Test test);
    Task<bool> DeleteAsync(int id);
}
