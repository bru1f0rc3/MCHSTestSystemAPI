using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.ReportsController;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : AuthorizedControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("my-stats")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetMyStatistics()
    {
        var result = await _reportService.GetUserStatisticsAsync(GetUserId());
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("Статистика не найдена"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetUserStatistics(int userId)
    {
        var result = await _reportService.GetUserStatisticsAsync(userId);
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("Статистика не найдена"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }
    [HttpGet("overall")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetOverallStatistics()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
    [HttpGet("tests")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestStatisticsDto>>>> GetTestsStatistics(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var allStats = await _reportService.GetTestStatisticsAsync();
        var pagedStats = allStats
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalCount = allStats.Count();
        var pagedResponse = new PagedResponse<TestStatisticsDto>
        {
            Items = pagedStats,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResponse<TestStatisticsDto>>.Ok(pagedResponse));
    }
    [HttpGet("users-performance")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserPerformanceDto>>>> GetUsersPerformance(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var users = await _reportService.GetUsersPerformanceAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<UserPerformanceDto>>.Ok(users));
    }
    [HttpGet("dashboard")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
    [HttpGet("test-statistics")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TestStatisticsDto>>>> GetTestStatistics()
    {
        var result = await _reportService.GetTestStatisticsAsync();
        return Ok(ApiResponse<IEnumerable<TestStatisticsDto>>.Ok(result));
    }
    [HttpGet("detailed")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<DetailedReportDto>>> GetDetailedReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? userId = null,
        [FromQuery] int? testId = null)
    {
        try
        {
            var result = await _reportService.GetDetailedReportAsync(startDate, endDate, userId, testId);
            return Ok(ApiResponse<DetailedReportDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResponse<DetailedReportDto>.Fail($"Ошибка построения отчёта: {ex.Message}"));
        }
    }
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<ReportDto>>.Ok(result));
    }
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(int id)
    {
        var result = await _reportService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<ReportDto>.Fail("Отчет не найден"));

        return Ok(ApiResponse<ReportDto>.Ok(result));
    }
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create([FromBody] CreateReportRequest request)
    {
        var result = await _reportService.CreateAsync(request, GetUserId());
        if (result == null)
            return BadRequest(ApiResponse<ReportDto>.Fail("Не удалось создать отчет"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ReportDto>.Ok(result, "Отчет создан"));
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _reportService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Отчет не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Отчет удален"));
    }
}
