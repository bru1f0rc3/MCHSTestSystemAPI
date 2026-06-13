using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.RoleService;

public class RoleService(IDbConnectionFactory db) : IRoleService
{
    /// <summary>
    /// Возвращает список всех ролей из базы данных
    /// </summary>
    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        using var connection = db.CreateConnection();
        return await connection.QueryAsync<Role>("SELECT id, name FROM roles ORDER BY id");
    }
}
