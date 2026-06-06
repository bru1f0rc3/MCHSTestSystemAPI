using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<PagedResponse<UserDto>> GetAllAsync(int page, int pageSize);
    Task<UserDto?> CreateAsync(CreateUserRequest request, bool callerIsSuperAdmin);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request, bool callerIsSuperAdmin);
    Task<bool> DeleteAsync(int id, bool callerIsSuperAdmin);
}
