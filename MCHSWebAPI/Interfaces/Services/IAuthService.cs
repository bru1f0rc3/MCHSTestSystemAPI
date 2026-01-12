using MCHSWebAPI.Models;
using MCHSWebAPI.DTOs.Auth;

namespace MCHSWebAPI.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RegisterGuestAsync(string deviceId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    string GenerateToken(User user);
}
