using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Interfaces;

public interface IReportService
{
    Task<ReportDto?> GetByIdAsync(int id);
    Task<PagedResponse<ReportDto>> GetAllAsync(int page, int pageSize);
    Task<ReportDto?> CreateAsync(CreateReportRequest request, int createdBy);
    Task<bool> DeleteAsync(int id);
    Task<DashboardDto> GetDashboardAsync();
    Task<IEnumerable<TestStatisticsDto>> GetTestStatisticsAsync();
    Task<UserStatisticsDto?> GetUserStatisticsAsync(int userId);
    Task<PagedResponse<UserPerformanceDto>> GetUsersPerformanceAsync(int page, int pageSize);
    Task<DetailedReportDto> GetDetailedReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int? userId,
        int? testId);
}
