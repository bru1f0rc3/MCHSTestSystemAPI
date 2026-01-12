using MCHSWebAPI.Models;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Users;
using MCHSWebAPI.DTOs.Common;
using BCrypt.Net;

namespace MCHSWebAPI.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<PagedResponse<UserDto>> GetAllAsync(int page, int pageSize)
    {
        var users = await _userRepository.GetAllAsync(page, pageSize);
        var totalCount = await _userRepository.GetTotalCountAsync();

        return new PagedResponse<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> CreateAsync(CreateUserRequest request)
    {
        if (await _userRepository.ExistsAsync(request.Username))
            return null;

        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null) return null;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId
        };

        user.Id = await _userRepository.CreateAsync(user);
        user.RoleName = role.Name;
        user.CreatedAt = DateTime.UtcNow;

        return MapToDto(user);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        if (request.Username != null)
        {
            if (request.Username != user.Username && await _userRepository.ExistsAsync(request.Username))
                return false;
            user.Username = request.Username;
        }

        if (request.RoleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId.Value);
            if (role == null) return false;
            user.RoleId = request.RoleId.Value;
        }

        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _userRepository.DeleteAsync(id);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.RoleName ?? "unknown",
            CreatedAt = user.CreatedAt
        };
    }
}
