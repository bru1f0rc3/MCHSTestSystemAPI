using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Services.LectureService.LectureService;

namespace MCHSWebAPI.Controllers.LectureController;

[ApiController]
[Route("api/[controller]")]
public class LecturesController : ControllerBase
{
    private readonly ILectureService _lectureService;

    public LecturesController(ILectureService lectureService)
    {
        _lectureService = lectureService;
    }
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
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<LectureDto>>> GetById(int id)
    {
        var result = await _lectureService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<LectureDto>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<LectureDto>.Ok(result));
    }
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<LectureDto>>> Create([FromBody] CreateLectureRequest request)
    {
        var result = await _lectureService.CreateAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<LectureDto>.Fail("Не удалось создать лекцию"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<LectureDto>.Ok(result, "Лекция создана"));
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateLectureRequest request)
    {
        var result = await _lectureService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Лекция не найдена"));

        return Ok(ApiResponse<bool>.Ok(true, "Лекция обновлена"));
    }
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
