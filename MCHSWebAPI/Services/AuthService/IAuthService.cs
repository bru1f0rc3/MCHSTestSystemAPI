using MCHSWebAPI.DTOs;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.AuthService.AuthService;
public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RegisterGuestAsync(string deviceId);
    Task<GuestStatusResponse> GetGuestStatusAsync(string deviceId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<string?> RequestPasswordResetCodeAsync(string loginOrEmail);
    Task<bool> ConfirmPasswordResetAsync(string loginOrEmail, string code, string newPassword);
    Task<string?> SendChangePasswordCodeAsync(int userId);
    Task<string?> SendDeleteAccountCodeAsync(int userId);
    Task<bool> DeleteCurrentUserAsync(int userId, string code);
    string GenerateToken(User user);
}
