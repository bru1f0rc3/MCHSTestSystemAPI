using MCHSWebAPI.DTOs;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RegisterGuestAsync(string deviceId);
    Task<GuestStatusResponse> GetGuestStatusAsync(string deviceId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<bool> DeleteCurrentUserAsync(int userId);
    string GenerateToken(User user);
}
