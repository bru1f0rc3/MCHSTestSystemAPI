using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using System.Security.Claims;
using MCHSWebAPI.Services.TestService.TestService;

namespace MCHSWebAPI.Controllers.TestController;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly ITestService _testService;

    public TestsController(ITestService testService)
    {
        _testService = testService;
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _testService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestDto>>.Ok(result));
    }
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TestDto>>> GetById(int id)
    {
        var result = await _testService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<TestDto>.Fail("Тест не найден"));

        return Ok(ApiResponse<TestDto>.Ok(result));
    }
    [HttpGet("{id}/full")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TestDetailDto>>> GetByIdWithQuestions(int id)
    {
        var isAdmin = User.IsInRole("admin");
        var result = await _testService.GetByIdWithQuestionsAsync(id, isAdmin);
        if (result == null)
            return NotFound(ApiResponse<TestDetailDto>.Fail("Тест не найден"));

        return Ok(ApiResponse<TestDetailDto>.Ok(result));
    }
    [HttpGet("by-lecture/{lectureId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<TestDto>>>> GetByLectureId(int lectureId)
    {
        var result = await _testService.GetByLectureIdAsync(lectureId);
        return Ok(ApiResponse<IEnumerable<TestDto>>.Ok(result));
    }
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<TestDto>>> Create([FromBody] CreateTestRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _testService.CreateAsync(request, userId);
        if (result == null)
            return BadRequest(ApiResponse<TestDto>.Fail("Не удалось создать тест"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<TestDto>.Ok(result, "Тест создан"));
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateTestRequest request)
    {
        var result = await _testService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Тест не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Тест обновлен"));
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _testService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Тест не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Тест удален"));
    }
    [HttpPost("{testId}/questions")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> AddQuestion(int testId, [FromBody] CreateQuestionRequest request)
    {
        var result = await _testService.AddQuestionAsync(testId, request);
        if (result == null)
            return BadRequest(ApiResponse<QuestionDto>.Fail("Не удалось добавить вопрос"));

        return Ok(ApiResponse<QuestionDto>.Ok(result, "Вопрос добавлен"));
    }
    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateQuestion(int questionId, [FromBody] UpdateQuestionRequest request)
    {
        var result = await _testService.UpdateQuestionAsync(questionId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Вопрос не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Вопрос обновлен"));
    }
    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteQuestion(int questionId)
    {
        var result = await _testService.DeleteQuestionAsync(questionId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Вопрос не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Вопрос удален"));
    }
    [HttpPost("questions/{questionId}/answers")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<AnswerDto>>> AddAnswer(int questionId, [FromBody] CreateAnswerRequest request)
    {
        var result = await _testService.AddAnswerAsync(questionId, request);
        if (result == null)
            return BadRequest(ApiResponse<AnswerDto>.Fail("Не удалось добавить ответ"));

        return Ok(ApiResponse<AnswerDto>.Ok(result, "Ответ добавлен"));
    }
    [HttpPut("answers/{answerId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAnswer(int answerId, [FromBody] UpdateAnswerRequest request)
    {
        var result = await _testService.UpdateAnswerAsync(answerId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Ответ не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ обновлен"));
    }
    [HttpDelete("answers/{answerId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAnswer(int answerId)
    {
        var result = await _testService.DeleteAnswerAsync(answerId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Ответ не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ удален"));
    }
    [HttpPost("import-from-pdf")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<TestDto>>> ImportFromPdf([FromForm] ImportTestFromPdfDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized(ApiResponse<TestDto>.Fail("Пользователь не авторизован"));

        try
        {
            using var stream = request.PdfFile.OpenReadStream();
            var result = await _testService.ImportFromPdfAsync(
                request.LectureId,
                request.Title,
                request.Description,
                request.TimeLimitMinutes,
                stream,
                userId);

            if (result == null)
                return BadRequest(ApiResponse<TestDto>.Fail("Не удалось импортировать тест из PDF"));

            return Ok(ApiResponse<TestDto>.Ok(result, "Тест успешно импортирован из PDF"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TestDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TestDto>.Fail($"Ошибка при импорте: {ex.Message}"));
        }
    }
    [HttpGet("available")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestDto>>>> GetAvailable(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _testService.GetAvailableForUserAsync(userId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestDto>>.Ok(result));
    }
}
