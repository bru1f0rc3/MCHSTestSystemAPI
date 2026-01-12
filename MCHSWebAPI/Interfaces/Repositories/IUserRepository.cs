using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByDeviceIdAsync(string deviceId);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> UpdateGuestToRegisteredAsync(int userId, string username, string passwordHash, string? email, int newRoleId);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string username);
}
