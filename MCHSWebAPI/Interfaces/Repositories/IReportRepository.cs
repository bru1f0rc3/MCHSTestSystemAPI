using MCHSWebAPI.DTOs.Common;
using MCHSWebAPI.DTOs.Reports;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(int id);
    Task<IEnumerable<Report>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Report>> GetByDateRangeAsync(DateOnly from, DateOnly to);
    Task<int> GetTotalCountAsync();
    Task<int> CreateAsync(Report report);
    Task<bool> DeleteAsync(int id);
    
    // Statistics queries
    Task<DashboardDto> GetDashboardAsync();
    Task<IEnumerable<TestStatisticsDto>> GetTestStatisticsAsync();
    Task<ReportStatistics> GetStatisticsAsync(DateOnly? from, DateOnly? to, int? testId, int? userId);
    Task<IEnumerable<UserStatistics>> GetUserStatisticsAsync(DateOnly? from, DateOnly? to, int? testId);
    Task<UserStatisticsDto?> GetUserStatisticsByIdAsync(int userId);
    Task<PagedResponse<UserPerformanceDto>> GetUsersPerformanceAsync(int page, int pageSize);
}
