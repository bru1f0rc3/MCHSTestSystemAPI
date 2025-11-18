using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;
using MCHSProject.DTO.Users;
using System.Threading.Tasks;

namespace MCHSProject.Services.Users
{
    public class UserService
    {
        private readonly DBConnect _dbConnect;

        public UserService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM users";
            return await connection.QueryAsync<User>(sql);
        }

        public async Task AddUserAsync(UserDTO user)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "INSERT INTO users (username, password_hash) VALUES (@Username, @PasswordHash)";
            await connection.ExecuteAsync(sql, new { user.Username, user.PasswordHash });
        }

        public async Task UpdateUserAsync(UserDTO user)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "UPDATE users SET password_hash = @PasswordHash WHERE username = @Username";
            await connection.ExecuteAsync(sql, new { user.PasswordHash, user.Username });
        }

        public async Task DeleteUserAsync(DeleteUserDTO user)
        {
            using var dbConnection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM users WHERE username = @Username";
            await dbConnection.ExecuteAsync(sql, new { user.Username });
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            using var conn = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM users WHERE id = @Id";
            return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }
    }
}
