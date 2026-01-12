using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Reports;
using MCHSWebAPI.DTOs.Common;
using System.Security.Claims;

namespace MCHSWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Получить статистику текущего пользователя
    /// </summary>
    [HttpGet("my-stats")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetMyStatistics()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reportService.GetUserStatisticsAsync(userId);
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("Статистика не найдена"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }

    /// <summary>
    /// Получить статистику пользователя по ID (только для админа)
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetUserStatistics(int userId)
    {
        var result = await _reportService.GetUserStatisticsAsync(userId);
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("Статистика не найдена"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }


    /// <summary>
    /// Получить общую статистику системы (для фронтенда)
    /// </summary>
    [HttpGet("overall")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetOverallStatistics()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    /// <summary>
    /// Получить статистику по тестам с пагинацией (для фронтенда)
    /// </summary>
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

    /// <summary>
    /// Получить статистику производительности пользователей (для фронтенда)
    /// </summary>
    [HttpGet("users-performance")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserPerformanceDto>>>> GetUsersPerformance(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Получаем все результаты пользователей и преобразуем их в UserPerformanceDto
        var users = await _reportService.GetUsersPerformanceAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<UserPerformanceDto>>.Ok(users));
    }

    /// <summary>
    /// Получить дашборд с общей статистикой
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    /// <summary>
    /// Получить статистику по тестам
    /// </summary>
    [HttpGet("test-statistics")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TestStatisticsDto>>>> GetTestStatistics()
    {
        var result = await _reportService.GetTestStatisticsAsync();
        return Ok(ApiResponse<IEnumerable<TestStatisticsDto>>.Ok(result));
    }

    /// <summary>
    /// Получить список сохраненных отчетов
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<ReportDto>>.Ok(result));
    }

    /// <summary>
    /// Получить отчет по ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(int id)
    {
        var result = await _reportService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<ReportDto>.Fail("Отчет не найден"));

        return Ok(ApiResponse<ReportDto>.Ok(result));
    }

    /// <summary>
    /// Создать новый отчет
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create([FromBody] CreateReportRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reportService.CreateAsync(request, userId);
        if (result == null)
            return BadRequest(ApiResponse<ReportDto>.Fail("Не удалось создать отчет"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ReportDto>.Ok(result, "Отчет создан"));
    }

    /// <summary>
    /// Удалить отчет
    /// </summary>
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
