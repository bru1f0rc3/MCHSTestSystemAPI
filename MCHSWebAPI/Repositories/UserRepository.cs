using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId, 
                   u.created_at as CreatedAt, r.name as RoleName
            FROM users u
            JOIN roles r ON u.role_id = r.id
            WHERE u.id = @Id";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId, 
                   u.device_id as DeviceId, u.email, u.created_at as CreatedAt, r.name as RoleName
            FROM users u
            JOIN roles r ON u.role_id = r.id
            WHERE u.username = @Username";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByDeviceIdAsync(string deviceId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId, 
                   u.device_id as DeviceId, u.email, u.created_at as CreatedAt, r.name as RoleName
            FROM users u
            JOIN roles r ON u.role_id = r.id
            WHERE u.device_id = @DeviceId";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { DeviceId = deviceId });
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT u.id, u.username, u.password_hash as PasswordHash, u.role_id as RoleId, 
                   u.created_at as CreatedAt, r.name as RoleName
            FROM users u
            JOIN roles r ON u.role_id = r.id
            ORDER BY u.created_at DESC
            LIMIT @PageSize OFFSET @Offset";
        return await connection.QueryAsync<User>(sql, new { PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
    }

    public async Task<int> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO users (username, password_hash, role_id, device_id, email)
            VALUES (@Username, @PasswordHash, @RoleId, @DeviceId, @Email)
            RETURNING id";
        return await connection.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE users 
            SET username = @Username, password_hash = @PasswordHash, role_id = @RoleId, 
                device_id = @DeviceId, email = @Email
            WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, user);
        return affected > 0;
    }

    public async Task<bool> UpdateGuestToRegisteredAsync(int userId, string username, string passwordHash, string? email, int newRoleId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE users 
            SET username = @Username, password_hash = @PasswordHash, email = @Email, role_id = @NewRoleId
            WHERE id = @UserId";
        var affected = await connection.ExecuteAsync(sql, new { UserId = userId, Username = username, PasswordHash = passwordHash, Email = email, NewRoleId = newRoleId });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM users WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<bool> ExistsAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)",
            new { Username = username });
    }
}
