using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;
using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Services.AuthService;

public class AuthService(IDbConnectionFactory db, IConfiguration config) : IAuthService
{
    private const string UserSelectColumns =
        @"u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId,
          u.device_id as DeviceId, u.email,
          u.last_name  as LastName,
          u.first_name as FirstName,
          u.patronymic as Patronymic,
          u.created_at as CreatedAt, r.name as RoleName";

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByUsernameAsync(request.Username.Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            var userByDevice = await GetUserByDeviceIdAsync(request.DeviceId);
            if (userByDevice != null && userByDevice.RoleName == "guest")
            {
                if (await UserExistsAsync(request.Username))
                    return null;

                var userRole = await GetRoleByNameAsync("user");
                if (userRole == null) return null;

                var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await UpgradeGuestAsync(
                    userByDevice.Id, request.Username, hash, userRole.Id,
                    request.LastName, request.FirstName, request.Patronymic);

                var updated = await GetUserByIdAsync(userByDevice.Id);
                return updated == null ? null : CreateAuthResponse(updated);
            }
            if (userByDevice != null && userByDevice.RoleName != "guest")
                throw new InvalidOperationException(
                    "На этом устройстве уже есть аккаунт. " +
                    "Войдите в него или удалите его, чтобы создать новый.");
        }

        if (await UserExistsAsync(request.Username))
            return null;

        var role = await GetRoleByNameAsync("user");
        if (role == null) return null;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            DeviceId = request.DeviceId,
            LastName = NullIfBlank(request.LastName),
            FirstName = NullIfBlank(request.FirstName),
            Patronymic = NullIfBlank(request.Patronymic)
        };

        user.Id = await CreateUserAsync(user);
        user.RoleName = role.Name;

        return CreateAuthResponse(user);
    }

    public async Task<GuestStatusResponse> GetGuestStatusAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return new GuestStatusResponse();

        var user = await GetUserByDeviceIdAsync(deviceId);
        if (user == null)
            return new GuestStatusResponse();

        return new GuestStatusResponse
        {
            HasExistingAccount = true,
            IsGuestAccount = string.Equals(user.RoleName, "guest", StringComparison.OrdinalIgnoreCase),
            Username = user.Username
        };
    }

    public async Task<AuthResponse?> RegisterGuestAsync(string deviceId)
    {
        if (!string.IsNullOrEmpty(deviceId))
        {
            var existing = await GetUserByDeviceIdAsync(deviceId);
            if (existing != null)
                return CreateAuthResponse(existing);
        }

        var role = await GetRoleByNameAsync("guest");
        if (role == null) return null;

        var user = new User
        {
            Username = $"guest_{Guid.NewGuid():N}"[..14],
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")[..12]),
            RoleId = role.Id,
            DeviceId = deviceId
        };

        user.Id = await CreateUserAsync(user);
        user.RoleName = role.Name;

        return CreateAuthResponse(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return false;
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("Новый пароль совпадает со старым");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        using var connection = db.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id",
            new { Hash = newHash, Id = userId });
        return affected > 0;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.RoleName ?? "guest",
            Email = user.Email,
            LastName = user.LastName,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        using var connection = db.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE users
              SET last_name  = @LastName,
                  first_name = @FirstName,
                  patronymic = @Patronymic
              WHERE id = @Id",
            new
            {
                Id = userId,
                LastName = NullIfBlank(request.LastName),
                FirstName = NullIfBlank(request.FirstName),
                Patronymic = NullIfBlank(request.Patronymic)
            });
        return await GetProfileAsync(userId);
    }

    public async Task<bool> DeleteCurrentUserAsync(int userId)
    {
        using var connection = db.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM users WHERE id = @Id", new { Id = userId });
        return affected > 0;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleName ?? "guest")
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(config.GetValue<int>("Jwt:ExpirationHours", 24)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var hours = config.GetValue<int>("Jwt:ExpirationHours", 24);
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.RoleName ?? "guest",
            Token = GenerateToken(user),
            ExpiresAt = DateTime.UtcNow.AddHours(hours)
        };
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<User?> GetUserByIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.id = @Id", new { Id = id });
    }

    private async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.username = @Username", new { Username = username });
    }

    private async Task<User?> GetUserByDeviceIdAsync(string deviceId)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.device_id = @DeviceId", new { DeviceId = deviceId });
    }

    private async Task<bool> UserExistsAsync(string username)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)",
            new { Username = username });
    }

    private async Task<int> CreateUserAsync(User user)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO users (username, password_hash, role_id, device_id, email,
                                  last_name, first_name, patronymic)
              VALUES (@Username, @PasswordHash, @RoleId, @DeviceId, @Email,
                      @LastName, @FirstName, @Patronymic)
              RETURNING id", user);
    }

    private async Task UpgradeGuestAsync(
        int userId, string username, string passwordHash, int newRoleId,
        string? lastName, string? firstName, string? patronymic)
    {
        using var connection = db.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE users SET username = @Username, password_hash = @PasswordHash,
                               role_id = @NewRoleId,
                               last_name  = COALESCE(@LastName,  last_name),
                               first_name = COALESCE(@FirstName, first_name),
                               patronymic = COALESCE(@Patronymic, patronymic)
              WHERE id = @UserId",
            new
            {
                UserId = userId,
                Username = username,
                PasswordHash = passwordHash,
                NewRoleId = newRoleId,
                LastName = NullIfBlank(lastName),
                FirstName = NullIfBlank(firstName),
                Patronymic = NullIfBlank(patronymic)
            });
    }

    private async Task<Role?> GetRoleByNameAsync(string name)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT id, name FROM roles WHERE name = @Name", new { Name = name });
    }
}
