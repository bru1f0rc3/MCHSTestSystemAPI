using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.Paths
{
    public class PathService
    {
        private readonly DBConnect _dbConnect;

        public PathService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<MediaPath?> GetPathByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM paths WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<MediaPath>(sql, new { Id = id });
        }

        public async Task<int> CreatePathAsync(string videoPath, string documentPath)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO paths (video_path, document_path) 
                       VALUES (@VideoPath, @DocumentPath) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, new { VideoPath = videoPath, DocumentPath = documentPath });
        }

        public async Task<bool> UpdatePathAsync(int id, string videoPath, string documentPath)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE paths 
                       SET video_path = @VideoPath, document_path = @DocumentPath 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id, VideoPath = videoPath, DocumentPath = documentPath });
            return rows > 0;
        }

        public async Task<bool> DeletePathAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM paths WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
