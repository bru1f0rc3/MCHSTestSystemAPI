using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.LectureController;

/// <summary>
/// Контроллер для работы с лекциями
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LecturesController : ControllerBase
{
    private readonly ILectureService _lectureService;

    /// <summary>
    /// Создаёт контроллер и получает сервис лекций
    /// </summary>
    /// <param name="lectureService">Сервис для работы с лекциями</param>
    public LecturesController(ILectureService lectureService)
    {
        _lectureService = lectureService;
    }

    /// <summary>
    /// Возвращает список лекций по страницам с возможностью поиска
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько лекций на странице (по умолчанию 20)</param>
    /// <param name="search">Текст для поиска (можно не указывать)</param>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<LectureListDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _lectureService.GetAllAsync(page, pageSize, search);
        return Ok(ApiResponse<PagedResponse<LectureListDto>>.Ok(result));
    }
    /// <summary>
    /// Возвращает одну лекцию по её номеру
    /// </summary>
    /// <param name="id">Номер (id) лекции</param>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<LectureDto>>> GetById(int id)
    {
        var result = await _lectureService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<LectureDto>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<LectureDto>.Ok(result));
    }
    /// <summary>
    /// Создаёт новую лекцию (только для админов)
    /// </summary>
    /// <param name="request">Данные новой лекции</param>
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<LectureDto>>> Create([FromBody] CreateLectureRequest request)
    {
        var result = await _lectureService.CreateAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<LectureDto>.Fail("Не удалось создать лекцию"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<LectureDto>.Ok(result, "Лекция создана"));
    }
    /// <summary>
    /// Обновляет лекцию по её номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) лекции</param>
    /// <param name="request">Новые данные лекции</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateLectureRequest request)
    {
        var result = await _lectureService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<bool>.Ok(true, "Лекция обновлена"));
    }
    /// <summary>
    /// Удаляет лекцию по её номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) лекции</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _lectureService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<bool>.Ok(true, "Лекция удалена"));
    }
}
