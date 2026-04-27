using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.RoleService.RoleService;

public class RoleService : IRoleService
{
    private readonly IDbConnectionFactory _db;

    public RoleService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        using var connection = _db.CreateConnection();
        return await connection.QueryAsync<Role>("SELECT id, name FROM roles ORDER BY id");
    }
}
