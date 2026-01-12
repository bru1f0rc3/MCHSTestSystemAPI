using MCHSWebAPI.DTOs.Reports;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Interfaces.Services;

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
}
