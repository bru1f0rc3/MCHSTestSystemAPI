using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT id, name FROM roles WHERE id = @Id", new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT id, name FROM roles WHERE name = @Name", new { Name = name });
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Role>("SELECT id, name FROM roles ORDER BY id");
    }
}
