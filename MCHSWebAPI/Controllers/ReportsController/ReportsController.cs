using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.ReportsController;

/// <summary>
/// Контроллер отчётов и статистики по тестам и пользователям
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : AuthorizedControllerBase
{
    private readonly IReportService _reportService;

    /// <summary>
    /// Создаёт контроллер и получает сервис отчётов
    /// </summary>
    /// <param name="reportService">Сервис для работы с отчётами и статистикой</param>
    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Возвращает статистику текущего пользователя
    /// </summary>
    [HttpGet("my-stats")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetMyStatistics()
    {
        var result = await _reportService.GetUserStatisticsAsync(GetUserId());
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("РЎС‚Р°С‚РёСЃС‚РёРєР° РЅРµ РЅР°Р№РґРµРЅР°"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает статистику конкретного пользователя (только для админов)
    /// </summary>
    /// <param name="userId">Номер пользователя, чья статистика нужна</param>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetUserStatistics(int userId)
    {
        var result = await _reportService.GetUserStatisticsAsync(userId);
        if (result == null)
            return NotFound(ApiResponse<UserStatisticsDto>.Fail("РЎС‚Р°С‚РёСЃС‚РёРєР° РЅРµ РЅР°Р№РґРµРЅР°"));
        return Ok(ApiResponse<UserStatisticsDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает общую статистику системы (только для админов)
    /// </summary>
    [HttpGet("overall")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetOverallStatistics()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает статистику по всем тестам по страницам (только для админов)
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько тестов на странице (по умолчанию 20)</param>
    [HttpGet("tests")]
    [Authorize(Roles = "admin,superadmin")]
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
    /// Возвращает результаты всех пользователей по страницам (только для админов)
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько пользователей на странице (по умолчанию 20)</param>
    [HttpGet("users-performance")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserPerformanceDto>>>> GetUsersPerformance(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var users = await _reportService.GetUsersPerformanceAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<UserPerformanceDto>>.Ok(users));
    }
    /// <summary>
    /// Возвращает данные для главной страницы — дашборда (только для админов)
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var result = await _reportService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает статистику по всем тестам одним списком (только для админов)
    /// </summary>
    [HttpGet("test-statistics")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TestStatisticsDto>>>> GetTestStatistics()
    {
        var result = await _reportService.GetTestStatisticsAsync();
        return Ok(ApiResponse<IEnumerable<TestStatisticsDto>>.Ok(result));
    }
    /// <summary>
    /// Строит подробный отчёт по результатам тестов с фильтрами (только для админов)
    /// </summary>
    /// <param name="startDate">Начало периода (можно не указывать)</param>
    /// <param name="endDate">Конец периода (можно не указывать)</param>
    /// <param name="userId">Номер пользователя для фильтра (можно не указывать)</param>
    /// <param name="testId">Номер теста для фильтра (можно не указывать)</param>
    [HttpGet("detailed")]
    [Authorize(Roles = "admin,superadmin")]
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
                ApiResponse<DetailedReportDto>.Fail($"РћС€РёР±РєР° РїРѕСЃС‚СЂРѕРµРЅРёСЏ РѕС‚С‡С‘С‚Р°: {ex.Message}"));
        }
    }
    /// <summary>
    /// Возвращает список сохранённых отчётов по страницам (только для админов)
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько отчётов на странице (по умолчанию 20)</param>
    [HttpGet]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<ReportDto>>.Ok(result));
    }
    /// <summary>
    /// Возвращает один отчёт по его номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) отчёта</param>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(int id)
    {
        var result = await _reportService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<ReportDto>.Fail("РћС‚С‡РµС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<ReportDto>.Ok(result));
    }
    /// <summary>
    /// Создаёт новый отчёт (только для админов)
    /// </summary>
    /// <param name="request">Данные для отчёта: заголовок, период и фильтры</param>
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create([FromBody] CreateReportRequest request)
    {
        var result = await _reportService.CreateAsync(request, GetUserId());
        if (result == null)
            return BadRequest(ApiResponse<ReportDto>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ СЃРѕР·РґР°С‚СЊ РѕС‚С‡РµС‚"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ReportDto>.Ok(result, "РћС‚С‡РµС‚ СЃРѕР·РґР°РЅ"));
    }
    /// <summary>
    /// Удаляет отчёт по его номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) отчёта</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _reportService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("РћС‚С‡РµС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "РћС‚С‡РµС‚ СѓРґР°Р»РµРЅ"));
    }
}
