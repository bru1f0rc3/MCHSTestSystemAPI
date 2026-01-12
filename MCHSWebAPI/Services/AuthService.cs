using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MCHSWebAPI.Models;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Auth;

namespace MCHSWebAPI.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepository, IRoleRepository roleRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _config = config;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            var guestUser = await _userRepository.GetByDeviceIdAsync(request.DeviceId);
            if (guestUser != null && guestUser.RoleName == "guest")
            {
                if (await _userRepository.ExistsAsync(request.Username))
                    return null;

                var userRole = await _roleRepository.GetByNameAsync("user")
                    ?? throw new InvalidOperationException("User role not found");

                var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await _userRepository.UpdateGuestToRegisteredAsync(guestUser.Id, request.Username, hash, request.Email, userRole.Id);

                var updated = await _userRepository.GetByIdAsync(guestUser.Id);
                return updated == null ? null : CreateAuthResponse(updated);
            }
        }

        if (await _userRepository.ExistsAsync(request.Username))
            return null;

        var role = await _roleRepository.GetByNameAsync("user")
            ?? throw new InvalidOperationException("User role not found");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            DeviceId = request.DeviceId,
            Email = request.Email
        };

        user.Id = await _userRepository.CreateAsync(user);
        user.RoleName = role.Name;

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> RegisterGuestAsync(string deviceId)
    {
        if (!string.IsNullOrEmpty(deviceId))
        {
            var existing = await _userRepository.GetByDeviceIdAsync(deviceId);
            if (existing != null)
                return CreateAuthResponse(existing);
        }

        var role = await _roleRepository.GetByNameAsync("guest")
            ?? throw new InvalidOperationException("Guest role not found");

        var user = new User
        {
            Username = $"guest_{Guid.NewGuid():N}"[..14],
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")[..12]),
            RoleId = role.Id,
            DeviceId = deviceId
        };

        user.Id = await _userRepository.CreateAsync(user);
        user.RoleName = role.Name;

        return CreateAuthResponse(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        return await _userRepository.UpdateAsync(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var hours = _config.GetValue<int>("Jwt:ExpirationHours", 24);
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.RoleName ?? "guest",
            Token = GenerateToken(user),
            ExpiresAt = DateTime.UtcNow.AddHours(hours)
        };
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleName ?? "guest")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:ExpirationHours", 24)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
