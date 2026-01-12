using MCHSWebAPI.DTOs.Users;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Interfaces.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<PagedResponse<UserDto>> GetAllAsync(int page, int pageSize);
    Task<UserDto?> CreateAsync(CreateUserRequest request);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteAsync(int id);
}
