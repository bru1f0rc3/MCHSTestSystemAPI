using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.Roles
{
    public class RoleService
    {
        private readonly DBConnect _dbConnect;

        public RoleService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM roles";
            return await connection.QueryAsync<Role>(sql);
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM roles WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Id = id });
        }

        public async Task<Role?> GetRoleByNameAsync(string name)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM roles WHERE name = @Name";
            return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Name = name });
        }
    }
}
