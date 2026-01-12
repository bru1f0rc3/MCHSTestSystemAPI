using MCHSWebAPI.DTOs.Common;
using MCHSWebAPI.DTOs.Reports;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.Models;
using System.Text.Json;

namespace MCHSWebAPI.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<ReportDto?> GetByIdAsync(int id)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null) return null;

        return MapToDto(report);
    }

    public async Task<PagedResponse<ReportDto>> GetAllAsync(int page, int pageSize)
    {
        var reports = await _reportRepository.GetAllAsync(page, pageSize);
        var totalCount = await _reportRepository.GetTotalCountAsync();

        return new PagedResponse<ReportDto>
        {
            Items = reports.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReportDto?> CreateAsync(CreateReportRequest request, int createdBy)
    {
        var statistics = await _reportRepository.GetStatisticsAsync(
            request.DateFrom, request.DateTo, request.TestId, request.UserId);
        
        var userStatistics = await _reportRepository.GetUserStatisticsAsync(
            request.DateFrom, request.DateTo, request.TestId);

        var reportContent = new ReportContent
        {
            Title = request.Title ?? $"Отчет от {DateOnly.FromDateTime(DateTime.Now)}",
            Description = request.Description,
            Statistics = statistics,
            UserResults = userStatistics.ToList()
        };

        var report = new Report
        {
            CreatedBy = createdBy,
            ReportDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Content = JsonSerializer.Serialize(reportContent)
        };

        var reportId = await _reportRepository.CreateAsync(report);
        return await GetByIdAsync(reportId);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _reportRepository.DeleteAsync(id);
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        return await _reportRepository.GetDashboardAsync();
    }

    public async Task<IEnumerable<TestStatisticsDto>> GetTestStatisticsAsync()
    {
        return await _reportRepository.GetTestStatisticsAsync();
    }

    private static ReportDto MapToDto(Report report)
    {
        ReportContent? content = null;
        if (!string.IsNullOrEmpty(report.Content))
        {
            try
            {
                content = JsonSerializer.Deserialize<ReportContent>(report.Content);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new ReportDto
        {
            Id = report.Id,
            CreatorUsername = report.CreatorUsername ?? "unknown",
            ReportDate = report.ReportDate,
            Content = content,
            CreatedAt = report.CreatedAt
        };
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(int userId)
    {
        return await _reportRepository.GetUserStatisticsByIdAsync(userId);
    }
    public async Task<PagedResponse<UserPerformanceDto>> GetUsersPerformanceAsync(int page, int pageSize)
    {
        return await _reportRepository.GetUsersPerformanceAsync(page, pageSize);
    }
}
