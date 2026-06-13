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

    /// <summary>
    /// Выполняет вход: проверяет логин и пароль и выдаёт токен
    /// </summary>
    /// <param name="request">Данные для входа: логин и пароль</param>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByUsernameAsync(request.Username.Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return CreateAuthResponse(user);
    }

    /// <summary>
    /// Регистрирует нового пользователя. Если с этого устройства был гостевой
    /// вход — превращает гостя в обычного пользователя
    /// </summary>
    /// <param name="request">Данные для регистрации: логин, пароль, ФИО, id устройства</param>
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

    /// <summary>
    /// Проверяет, есть ли на этом устройстве уже аккаунт и гостевой ли он
    /// </summary>
    /// <param name="deviceId">Идентификатор устройства, которое проверяем</param>
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

    /// <summary>
    /// Создаёт гостевой аккаунт для устройства. Если гость уже есть — возвращает его
    /// </summary>
    /// <param name="deviceId">Идентификатор устройства, для которого создаём гостя</param>
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

    /// <summary>
    /// Меняет пароль пользователя: проверяет старый пароль и сохраняет новый
    /// </summary>
    /// <param name="userId">Номер пользователя, который меняет пароль</param>
    /// <param name="request">Старый и новый пароль</param>
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

    /// <summary>
    /// Возвращает данные профиля пользователя (логин, ФИО, роль, почта)
    /// </summary>
    /// <param name="userId">Номер пользователя, чей профиль нужен</param>
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

    /// <summary>
    /// Обновляет ФИО в профиле пользователя
    /// </summary>
    /// <param name="userId">Номер пользователя, чей профиль меняем</param>
    /// <param name="request">Новые данные профиля: фамилия, имя, отчество</param>
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

    /// <summary>
    /// Удаляет аккаунт текущего пользователя
    /// </summary>
    /// <param name="userId">Номер пользователя, который удаляет свой аккаунт</param>
    public async Task<bool> DeleteCurrentUserAsync(int userId)
    {
        using var connection = db.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM users WHERE id = @Id", new { Id = userId });
        return affected > 0;
    }

    /// <summary>
    /// Создаёт JWT-токен для пользователя (его кладут в заголовок при запросах)
    /// </summary>
    /// <param name="user">Пользователь, для которого создаётся токен</param>
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

    /// <summary>
    /// Собирает ответ для клиента после успешного входа:
    /// данные пользователя, токен и срок его действия
    /// </summary>
    /// <param name="user">Пользователь, для которого готовим ответ</param>
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

    /// <summary>
    /// Возвращает null, если строка пустая, иначе обрезает пробелы по краям
    /// </summary>
    /// <param name="value">Строка, которую нужно проверить и почистить</param>
    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Находит пользователя в базе по его номеру
    /// </summary>
    /// <param name="id">Номер (id) пользователя, которого ищем</param>
    private async Task<User?> GetUserByIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.id = @Id", new { Id = id });
    }

    /// <summary>
    /// Находит пользователя в базе по его логину
    /// </summary>
    /// <param name="username">Логин пользователя, которого ищем</param>
    private async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.username = @Username", new { Username = username });
    }

    /// <summary>
    /// Находит пользователя в базе по идентификатору его устройства
    /// </summary>
    /// <param name="deviceId">Идентификатор устройства, по которому ищем</param>
    private async Task<User?> GetUserByDeviceIdAsync(string deviceId)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserSelectColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.device_id = @DeviceId", new { DeviceId = deviceId });
    }

    /// <summary>
    /// Проверяет, есть ли уже пользователь с таким логином
    /// </summary>
    /// <param name="username">Логин, который проверяем на занятость</param>
    private async Task<bool> UserExistsAsync(string username)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)",
            new { Username = username });
    }

    /// <summary>
    /// Добавляет нового пользователя в базу и возвращает его номер
    /// </summary>
    /// <param name="user">Данные пользователя, которого добавляем</param>
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

    /// <summary>
    /// Превращает гостевой аккаунт в обычный: задаёт логин, пароль, роль и ФИО
    /// </summary>
    /// <param name="userId">Номер гостевого пользователя, которого повышаем</param>
    /// <param name="username">Новый логин</param>
    /// <param name="passwordHash">Зашифрованный новый пароль</param>
    /// <param name="newRoleId">Номер новой роли (обычного пользователя)</param>
    /// <param name="lastName">Фамилия (можно не указывать)</param>
    /// <param name="firstName">Имя (можно не указывать)</param>
    /// <param name="patronymic">Отчество (можно не указывать)</param>
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

    /// <summary>
    /// Находит роль в базе по её названию (например, "user" или "guest")
    /// </summary>
    /// <param name="name">Название роли, которую ищем</param>
    private async Task<Role?> GetRoleByNameAsync(string name)
    {
        using var connection = db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT id, name FROM roles WHERE name = @Name", new { Name = name });
    }
}
