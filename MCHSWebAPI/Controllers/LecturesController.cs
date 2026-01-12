using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Lectures;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LecturesController : ControllerBase
{
    private readonly ILectureService _lectureService;

    public LecturesController(ILectureService lectureService)
    {
        _lectureService = lectureService;
    }

    /// <summary>
    /// Получить список всех лекций
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<LectureListDto>>>> GetAll(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _lectureService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<LectureListDto>>.Ok(result));
    }

    /// <summary>
    /// Получить лекцию по ID
    /// </summary>
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
    /// Создать новую лекцию (только админ)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<LectureDto>>> Create([FromBody] CreateLectureRequest request)
    {
        var result = await _lectureService.CreateAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<LectureDto>.Fail("Не удалось создать лекцию"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<LectureDto>.Ok(result, "Лекция создана"));
    }

    /// <summary>
    /// Обновить лекцию (только админ)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateLectureRequest request)
    {
        var result = await _lectureService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<bool>.Ok(true, "Лекция обновлена"));
    }

    /// <summary>
    /// Удалить лекцию (только админ)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _lectureService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<bool>.Ok(true, "Лекция удалена"));
    }
}
