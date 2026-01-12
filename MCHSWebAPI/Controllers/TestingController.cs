using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Testing;
using MCHSWebAPI.DTOs.Common;
using System.Security.Claims;

namespace MCHSWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestingController : ControllerBase
{
    private readonly ITestingService _testingService;

    public TestingController(ITestingService testingService)
    {
        _testingService = testingService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Начать прохождение теста
    /// </summary>
    [HttpPost("start/{testId}")]
    public async Task<ActionResult<ApiResponse<StartTestResponse>>> StartTest(int testId)
    {
        var userId = GetUserId();
        var result = await _testingService.StartTestAsync(testId, userId);
        if (result == null)
            return NotFound(ApiResponse<StartTestResponse>.Fail("Тест не найден"));

        return Ok(ApiResponse<StartTestResponse>.Ok(result, "Тест начат"));
    }

    /// <summary>
    /// Отправить ответ на вопрос
    /// </summary>
    [HttpPost("{testResultId}/answer")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitAnswer(
        int testResultId, 
        [FromBody] SubmitAnswerRequest request)
    {
        var userId = GetUserId();
        var result = await _testingService.SubmitAnswerAsync(testResultId, userId, request);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось сохранить ответ"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ сохранен"));
    }

    /// <summary>
    /// Отправить несколько ответов сразу
    /// </summary>
    [HttpPost("{testResultId}/answers")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitAnswers(
        int testResultId, 
        [FromBody] SubmitAnswersRequest request)
    {
        var userId = GetUserId();
        var result = await _testingService.SubmitAnswersAsync(testResultId, userId, request);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось сохранить ответы"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответы сохранены"));
    }

    /// <summary>
    /// Завершить тест и получить результат
    /// </summary>
    [HttpPost("{testResultId}/finish")]
    public async Task<ActionResult<ApiResponse<FinishTestResponse>>> FinishTest(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.FinishTestAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<FinishTestResponse>.Fail("Тест не найден или не принадлежит вам"));

        return Ok(ApiResponse<FinishTestResponse>.Ok(result, "Тест завершен"));
    }

    /// <summary>
    /// Получить краткий результат теста
    /// </summary>
    [HttpGet("result/{testResultId}")]
    public async Task<ActionResult<ApiResponse<TestResultDto>>> GetResult(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.GetTestResultAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<TestResultDto>.Fail("Результат не найден"));

        return Ok(ApiResponse<TestResultDto>.Ok(result));
    }

    /// <summary>
    /// Получить детальный результат теста с разбором ответов
    /// </summary>
    [HttpGet("result/{testResultId}/detail")]
    public async Task<ActionResult<ApiResponse<TestResultDetailDto>>> GetResultDetail(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.GetTestResultDetailAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<TestResultDetailDto>.Fail("Результат не найден"));

        return Ok(ApiResponse<TestResultDetailDto>.Ok(result));
    }

    /// <summary>
    /// Получить историю своих результатов
    /// </summary>
    [HttpGet("my-results")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestResultDto>>>> GetMyResults(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await _testingService.GetUserResultsAsync(userId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestResultDto>>.Ok(result));
    }

    /// <summary>
    /// Получить все результаты (только админ)
    /// </summary>
    [HttpGet("all-results")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestResultDto>>>> GetAllResults(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var result = await _testingService.GetAllResultsAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestResultDto>>.Ok(result));
    }
}
