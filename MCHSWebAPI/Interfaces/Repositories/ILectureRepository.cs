using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface ILectureRepository
{
    Task<Lecture?> GetByIdAsync(int id);
    Task<Lecture?> GetByIdWithPathAsync(int id);
    Task<IEnumerable<Lecture>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> CreateAsync(Lecture lecture, LearningPath? path);
    Task<bool> UpdateAsync(Lecture lecture, LearningPath? path);
    Task<bool> DeleteAsync(int id);
}
