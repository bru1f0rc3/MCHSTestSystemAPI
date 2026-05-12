using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<PagedResponse<UserDto>> GetAllAsync(int page, int pageSize);
    Task<UserDto?> CreateAsync(CreateUserRequest request);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteAsync(int id);
}
