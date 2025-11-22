using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.Lectures
{
    public class LectureService
    {
        private readonly DBConnect _dbConnect;

        public LectureService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Lecture>> GetAllLecturesAsync()
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM lectures ORDER BY created_at DESC";
            return await connection.QueryAsync<Lecture>(sql);
        }

        public async Task<Lecture?> GetLectureByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM lectures WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Lecture>(sql, new { Id = id });
        }

        public async Task<int> CreateLectureAsync(Lecture lecture)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO lectures (title, text_content, path_id, created_at) 
                       VALUES (@Title, @TextContent, @PathId, @CreatedAt) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, lecture);
        }

        public async Task<bool> UpdateLectureAsync(Lecture lecture)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE lectures 
                       SET title = @Title, text_content = @TextContent, path_id = @PathId 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, lecture);
            return rows > 0;
        }

        public async Task<bool> DeleteLectureAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM lectures WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
