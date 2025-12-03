using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;
using System.Text.Json;

namespace MCHSProject.Services.Reports
{
    public class ReportService
    {
        private readonly DBConnect _dbConnect;

        public ReportService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Report>> GetAllReportsAsync()
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM reports ORDER BY report_date DESC";
            return await connection.QueryAsync<Report>(sql);
        }

        public async Task<Report?> GetReportByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM reports WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Report>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Report>> GetReportsByUserIdAsync(int userId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM reports WHERE created_by = @UserId ORDER BY report_date DESC";
            return await connection.QueryAsync<Report>(sql, new { UserId = userId });
        }

        public async Task<int> CreateReportAsync(int userId, DateTime reportDate, object contentData)
        {
            using var connection = _dbConnect.CreateConnection();
            var jsonContent = JsonSerializer.Serialize(contentData);
            var sql = @"INSERT INTO reports (created_by, report_date, content, created_at) 
                       VALUES (@CreatedBy, @ReportDate, @Content::jsonb, @CreatedAt) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, 
                new { CreatedBy = userId, ReportDate = reportDate, Content = jsonContent, CreatedAt = DateTime.UtcNow });
        }

        public async Task<bool> DeleteReportAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM reports WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
