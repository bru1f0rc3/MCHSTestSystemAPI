using MCHSWebAPI.DTOs.Lectures;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Interfaces.Services;

public interface ILectureService
{
    Task<LectureDto?> GetByIdAsync(int id);
    Task<PagedResponse<LectureListDto>> GetAllAsync(int page, int pageSize);
    Task<LectureDto?> CreateAsync(CreateLectureRequest request);
    Task<bool> UpdateAsync(int id, UpdateLectureRequest request);
    Task<bool> DeleteAsync(int id);
}
