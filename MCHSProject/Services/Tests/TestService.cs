using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;
using MCHSProject.DTO.Tests;

namespace MCHSProject.Services.Tests
{
    public class TestService
    {
        private readonly DBConnect _dbConnect;

        public TestService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Test>> GetAllTestsAsync()
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM tests ORDER BY created_at DESC";
            return await connection.QueryAsync<Test>(sql);
        }

        public async Task<Test?> GetTestByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM tests WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Test>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Test>> GetTestsByLectureIdAsync(int lectureId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM tests WHERE lecture_id = @LectureId";
            return await connection.QueryAsync<Test>(sql, new { LectureId = lectureId });
        }

        public async Task<int> CreateTestAsync(CreateTestDTO dto)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO tests (lecture_id, title, description, created_by, created_at) 
                       VALUES (@LectureId, @Title, @Description, @CreatedBy, @CreatedAt) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                dto.LectureId, 
                dto.Title, 
                dto.Description, 
                dto.CreatedBy, 
                CreatedAt = DateTime.UtcNow 
            });
        }

        public async Task<bool> UpdateTestAsync(Test test)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE tests 
                       SET title = @Title, description = @Description 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, test);
            return rows > 0;
        }

        public async Task<bool> DeleteTestAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM tests WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
