using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id);
    Task<Question?> GetByIdWithAnswersAsync(int id);
    Task<IEnumerable<Question>> GetByTestIdAsync(int testId);
    Task<int> CreateAsync(Question question);
    Task<bool> UpdateAsync(Question question);
    Task<bool> DeleteAsync(int id);
    Task<int> GetCountByTestIdAsync(int testId);
    Task<Dictionary<int, int>> GetCountsByTestIdsAsync(IEnumerable<int> testIds);
}
