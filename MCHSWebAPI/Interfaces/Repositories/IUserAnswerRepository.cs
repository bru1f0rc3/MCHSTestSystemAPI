using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IUserAnswerRepository
{
    Task<UserAnswer?> GetByIdAsync(int id);
    Task<IEnumerable<UserAnswer>> GetByTestResultIdAsync(int testResultId);
    Task<UserAnswer?> GetByTestResultAndQuestionAsync(int testResultId, int questionId);
    Task<int> CreateAsync(UserAnswer userAnswer);
    Task<bool> UpdateAsync(UserAnswer userAnswer);
    Task<bool> DeleteAsync(int id);
    Task<int> GetCorrectCountByTestResultIdAsync(int testResultId);
}
