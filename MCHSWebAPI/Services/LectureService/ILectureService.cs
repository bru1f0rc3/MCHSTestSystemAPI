using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Services.LectureService.LectureService;
public interface ILectureService
{
    Task<LectureDto?> GetByIdAsync(int id);
    Task<PagedResponse<LectureListDto>> GetAllAsync(int page, int pageSize, string? search = null);
    Task<LectureDto?> CreateAsync(CreateLectureRequest request);
    Task<bool> UpdateAsync(int id, UpdateLectureRequest request);
    Task<bool> DeleteAsync(int id);
}
