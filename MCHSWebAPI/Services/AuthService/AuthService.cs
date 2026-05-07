using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Helpers;
using MCHSWebAPI.Models;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Services.VerificationService;

namespace MCHSWebAPI.Services.AuthService.AuthService;

public class AuthService : IAuthService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly IVerificationService _verification;

    public AuthService(IDbConnectionFactory db, IConfiguration config, IVerificationService verification)
    {
        _db = db;
        _config = config;
        _verification = verification;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var key = request.Username.Trim();
        var user = key.Contains('@')
            ? await GetUserByEmailAsync(key)
            : await GetUserByUsernameAsync(key);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!await _verification.VerifyCodeAsync(normalizedEmail, request.VerificationCode, "registration"))
            return null;
        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            var userByDevice = await GetUserByDeviceIdAsync(request.DeviceId);
            if (userByDevice != null && userByDevice.RoleName == "guest")
            {
                if (await UserExistsAsync(request.Username))
                    return null;
                if (await EmailInUseByAnotherAsync(normalizedEmail, userByDevice.Id))
                    return null;

                var userRole = await GetRoleByNameAsync("user");
                if (userRole == null) return null;

                var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await UpdateGuestToRegisteredAsync(
                    userByDevice.Id, request.Username, hash, normalizedEmail, userRole.Id,
                    request.LastName, request.FirstName, request.Patronymic);

                var updated = await GetUserByIdAsync(userByDevice.Id);
                return updated == null ? null : CreateAuthResponse(updated);
            }
            if (userByDevice != null && userByDevice.RoleName != "guest")
                throw new InvalidOperationException(
                    "Здравствуйте, у вас уже существует аккаунт на этом устройстве. " +
                    "Зайдите в него. Если хотите создать новый аккаунт, удалите предыдущий аккаунт.");
        }

        if (await UserExistsAsync(request.Username))
            return null;
        if (await EmailInUseByAnotherAsync(normalizedEmail))
            return null;

        var role = await GetRoleByNameAsync("user");
        if (role == null) return null;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            DeviceId = request.DeviceId,
            Email = normalizedEmail,
            EmailVerified = true,
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName!.Trim(),
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName!.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic!.Trim()
        };

        user.Id = await CreateUserAsync(user);
        user.RoleName = role.Name;

        return CreateAuthResponse(user);
    }

    public async Task<GuestStatusResponse> GetGuestStatusAsync(string deviceId)
    {
        var noAccount = new GuestStatusResponse { HasExistingAccount = false, IsGuestAccount = false };

        if (string.IsNullOrWhiteSpace(deviceId))
            return noAccount;

        var user = await GetUserByDeviceIdAsync(deviceId);
        if (user == null)
            return noAccount;

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
        if (string.IsNullOrWhiteSpace(user.Email))
            return false;
        if (!await _verification.VerifyCodeAsync(user.Email.Trim().ToLowerInvariant(), request.VerificationCode, "password_change"))
            return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        using var connection = _db.CreateConnection();
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
            EmailVerified = user.EmailVerified,
            LastName = user.LastName,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE users
              SET last_name  = @LastName,
                  first_name = @FirstName,
                  patronymic = @Patronymic
              WHERE id = @Id",
            new
            {
                Id = userId,
                LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName!.Trim(),
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName!.Trim(),
                Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic!.Trim()
            });
        return await GetProfileAsync(userId);
    }

    public async Task<string?> RequestPasswordResetCodeAsync(string loginOrEmail)
    {
        var user = await GetUserByLoginOrEmailAsync(loginOrEmail);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return null;

        var email = user.Email.Trim().ToLowerInvariant();
        var sent = await _verification.SendCodeAsync(email, "password_reset");
        return sent ? EmailHelper.MaskEmail(email) : null;
    }

    public async Task<bool> ConfirmPasswordResetAsync(string loginOrEmail, string code, string newPassword)
    {
        var user = await GetUserByLoginOrEmailAsync(loginOrEmail);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return false;

        var email = user.Email.Trim().ToLowerInvariant();
        return await _verification.ResetPasswordAsync(email, code, newPassword);
    }

    public async Task<string?> SendChangePasswordCodeAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return null;
        var email = user.Email.Trim().ToLowerInvariant();
        var sent = await _verification.SendCodeAsync(email, "password_change");
        return sent ? EmailHelper.MaskEmail(email) : null;
    }

    public async Task<string?> SendDeleteAccountCodeAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return null;
        var email = user.Email.Trim().ToLowerInvariant();
        var sent = await _verification.SendCodeAsync(email, "account_delete");
        return sent ? EmailHelper.MaskEmail(email) : null;
    }

    public async Task<bool> DeleteCurrentUserAsync(int userId, string code)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return false;

        var email = user.Email.Trim().ToLowerInvariant();
        var verified = await _verification.VerifyCodeAsync(email, code, "account_delete");
        if (!verified) return false;

        using var connection = _db.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM users WHERE id = @Id", new { Id = userId });
        return affected > 0;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

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

    private const string UserSelectColumns =
        @"u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId,
          u.device_id as DeviceId, u.email,
          u.email_verified as EmailVerified,
          u.pending_email as PendingEmail,
          u.pending_email_verified as PendingEmailVerified,
          u.last_name  as LastName,
          u.first_name as FirstName,
          u.patronymic as Patronymic,
          u.created_at as CreatedAt, r.name as RoleName";

    private async Task<User?> GetUserByIdAsync(int id)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.id = @Id", new { Id = id });
    }

    private async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.username = @Username", new { Username = username });
    }

    private async Task<User?> GetUserByEmailAsync(string email)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE lower(u.email) = lower(@Email)",
            new { Email = email });
    }

    private async Task<User?> GetUserByLoginOrEmailAsync(string loginOrEmail)
    {
        if (string.IsNullOrWhiteSpace(loginOrEmail)) return null;
        var key = loginOrEmail.Trim();
        if (key.Contains('@'))
        {
            return await GetUserByEmailAsync(key);
        }
        return await GetUserByUsernameAsync(key);
    }

    private async Task<User?> GetUserByDeviceIdAsync(string deviceId)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.device_id = @DeviceId", new { DeviceId = deviceId });
    }

    private async Task<bool> UserExistsAsync(string username)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)",
            new { Username = username });
    }

    private async Task<bool> EmailInUseByAnotherAsync(string email, int? excludeUserId = null)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 FROM users
                WHERE lower(email) = lower(@Email)
                  AND (@ExcludeUserId IS NULL OR id <> @ExcludeUserId)
            )",
            new { Email = email, ExcludeUserId = excludeUserId });
    }

    private async Task<int> CreateUserAsync(User user)
    {
        using var connection = _db.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO users (username, password_hash, role_id, device_id, email,
                                  email_verified, pending_email, pending_email_verified,
                                  last_name, first_name, patronymic)
              VALUES (@Username, @PasswordHash, @RoleId, @DeviceId, @Email,
                      @EmailVerified, @PendingEmail, @PendingEmailVerified,
                      @LastName, @FirstName, @Patronymic)
              RETURNING id", user);
    }

    private async Task UpdateGuestToRegisteredAsync(
        int userId, string username, string passwordHash, string? email, int newRoleId,
        string? lastName, string? firstName, string? patronymic)
    {
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE users SET username = @Username, password_hash = @PasswordHash,
                               email = @Email, email_verified = TRUE, role_id = @NewRoleId,
                               pending_email = NULL, pending_email_verified = FALSE,
                               last_name  = COALESCE(@LastName,  last_name),
                               first_name = COALESCE(@FirstName, first_name),
                               patronymic = COALESCE(@Patronymic, patronymic)
              WHERE id = @UserId",
            new
            {
                UserId = userId,
                Username = username,
                PasswordHash = passwordHash,
                Email = email,
                NewRoleId = newRoleId,
                LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName!.Trim(),
                FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName!.Trim(),
                Patronymic = string.IsNullOrWhiteSpace(patronymic) ? null : patronymic!.Trim()
            });
    }

    private async Task<Role?> GetRoleByNameAsync(string name)
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT id, name FROM roles WHERE name = @Name", new { Name = name });
    }

}
