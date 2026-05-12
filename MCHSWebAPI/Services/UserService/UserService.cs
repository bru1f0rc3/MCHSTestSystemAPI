using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.UserService;

public class UserService(IDbConnectionFactory db) : IUserService
{
    private const string UserColumns =
        @"u.id, u.username, u.device_id as DeviceId, u.role_id as RoleId, u.email,
          u.last_name as LastName, u.first_name as FirstName, u.patronymic as Patronymic,
          u.created_at as CreatedAt, r.name as RoleName";

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            $@"SELECT {UserColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               WHERE u.id = @Id",
            new { Id = id });

        return user == null ? null : MapToDto(user);
    }

    public async Task<PagedResponse<UserDto>> GetAllAsync(int page, int pageSize)
    {
        using var connection = db.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");

        var users = await connection.QueryAsync<User>(
            $@"SELECT {UserColumns}
               FROM users u JOIN roles r ON u.role_id = r.id
               ORDER BY u.created_at DESC
               LIMIT @PageSize OFFSET @Offset",
            new { PageSize = pageSize, Offset = (page - 1) * pageSize });

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
        using var connection = db.CreateConnection();

        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)",
            new { request.Username });

        if (exists) return null;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var userId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO users (username, password_hash, role_id, last_name, first_name, patronymic)
              VALUES (@Username, @PasswordHash, @RoleId, @LastName, @FirstName, @Patronymic)
              RETURNING id",
            new
            {
                request.Username,
                PasswordHash = passwordHash,
                request.RoleId,
                LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName!.Trim(),
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName!.Trim(),
                Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic!.Trim()
            });

        return await GetByIdAsync(userId);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request)
    {
        using var connection = db.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            @"SELECT u.id, u.role_id AS RoleId, r.name AS RoleName
              FROM users u JOIN roles r ON u.role_id = r.id
              WHERE u.id = @Id", new { Id = id });
        if (user == null) return false;

        if (request.RoleId.HasValue
            && user.RoleName == "admin"
            && request.RoleId.Value != user.RoleId
            && await IsLastAdminAsync(id))
        {
            throw new InvalidOperationException("Нельзя понизить роль единственного администратора");
        }

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            setClauses.Add("username = @Username");
            parameters.Add("Username", request.Username);
        }
        if (request.RoleId.HasValue)
        {
            setClauses.Add("role_id = @RoleId");
            parameters.Add("RoleId", request.RoleId.Value);
        }
        if (request.LastName != null)
        {
            setClauses.Add("last_name = @LastName");
            parameters.Add("LastName", string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim());
        }
        if (request.FirstName != null)
        {
            setClauses.Add("first_name = @FirstName");
            parameters.Add("FirstName", string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim());
        }
        if (request.Patronymic != null)
        {
            setClauses.Add("patronymic = @Patronymic");
            parameters.Add("Patronymic", string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim());
        }

        if (setClauses.Count == 0) return false;

        await connection.ExecuteAsync(
            $"UPDATE users SET {string.Join(", ", setClauses)} WHERE id = @Id", parameters);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (await IsLastAdminAsync(id))
            throw new InvalidOperationException("Нельзя удалить единственного администратора");

        using var connection = db.CreateConnection();
        return await connection.ExecuteAsync("DELETE FROM users WHERE id = @Id", new { Id = id }) > 0;
    }

    private async Task<bool> IsLastAdminAsync(int userId)
    {
        using var connection = db.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 FROM users u JOIN roles r ON u.role_id = r.id
                WHERE u.id = @Id AND r.name = 'admin'
              ) AND (
                SELECT COUNT(*) FROM users u JOIN roles r ON u.role_id = r.id
                WHERE r.name = 'admin'
              ) <= 1",
            new { Id = userId });
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.RoleName ?? "unknown",
            Email = user.Email,
            LastName = user.LastName,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            CreatedAt = user.CreatedAt
        };
    }
}
