using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(int id);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
    Task<Answer?> GetCorrectAnswerByQuestionIdAsync(int questionId);
    Task<Dictionary<int, Answer>> GetCorrectAnswersByQuestionIdsAsync(IEnumerable<int> questionIds);
    Task<int> CreateAsync(Answer answer);
    Task<bool> UpdateAsync(Answer answer);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByQuestionIdAsync(int questionId);
}
