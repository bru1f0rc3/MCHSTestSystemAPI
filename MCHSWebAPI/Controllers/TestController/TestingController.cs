using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using System.Security.Claims;
using MCHSWebAPI.Services.TestService.TestService;

namespace MCHSWebAPI.Controllers.TestController;

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
    [HttpPost("start/{testId}")]
    public async Task<ActionResult<ApiResponse<StartTestResponse>>> StartTest(int testId)
    {
        var userId = GetUserId();
        var result = await _testingService.StartTestAsync(testId, userId);
        if (result == null)
            return NotFound(ApiResponse<StartTestResponse>.Fail("Тест не найден"));

        return Ok(ApiResponse<StartTestResponse>.Ok(result, "Тест начат"));
    }
    [HttpPost("{testResultId}/answer")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitAnswer(
        int testResultId,
        [FromBody] SubmitAnswerRequest request)
    {
        var userId = GetUserId();
        var result = await _testingService.SubmitAnswerAsync(testResultId, userId, request);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось сохранить ответ (возможно, время вышло)"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ сохранен"));
    }
    [HttpPost("{testResultId}/answers")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitAnswers(
        int testResultId,
        [FromBody] SubmitAnswersRequest request)
    {
        var userId = GetUserId();
        var result = await _testingService.SubmitAnswersAsync(testResultId, userId, request);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось сохранить ответы (возможно, время вышло)"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответы сохранены"));
    }
    [HttpPost("{testResultId}/finish")]
    public async Task<ActionResult<ApiResponse<FinishTestResponse>>> FinishTest(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.FinishTestAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<FinishTestResponse>.Fail("Тест не найден или не принадлежит вам"));

        return Ok(ApiResponse<FinishTestResponse>.Ok(result, "Тест завершен"));
    }
    [HttpPost("{testResultId}/cheat-attempt")]
    public async Task<ActionResult<ApiResponse<bool>>> ReportCheatAttempt(
        int testResultId,
        [FromBody] ReportCheatAttemptRequest? request)
    {
        var userId = GetUserId();
        var result = await _testingService.RegisterCheatAttemptAsync(
            testResultId, userId, request ?? new ReportCheatAttemptRequest());
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось зафиксировать событие"));

        return Ok(ApiResponse<bool>.Ok(true, "Событие зафиксировано"));
    }
    [HttpGet("result/{testResultId}")]
    public async Task<ActionResult<ApiResponse<TestResultDto>>> GetResult(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.GetTestResultAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<TestResultDto>.Fail("Результат не найден"));

        return Ok(ApiResponse<TestResultDto>.Ok(result));
    }
    [HttpGet("result/{testResultId}/detail")]
    public async Task<ActionResult<ApiResponse<TestResultDetailDto>>> GetResultDetail(int testResultId)
    {
        var userId = GetUserId();
        var result = await _testingService.GetTestResultDetailAsync(testResultId, userId);
        if (result == null)
            return NotFound(ApiResponse<TestResultDetailDto>.Fail("Результат не найден"));

        return Ok(ApiResponse<TestResultDetailDto>.Ok(result));
    }
    [HttpGet("my-results")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestResultDto>>>> GetMyResults(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var result = await _testingService.GetUserResultsAsync(userId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestResultDto>>.Ok(result));
    }
    [HttpGet("all-results")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestResultDto>>>> GetAllResults(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchQuery = null)
    {
        var result = await _testingService.GetAllResultsAsync(page, pageSize, startDate, endDate, searchQuery);
        return Ok(ApiResponse<PagedResponse<TestResultDto>>.Ok(result));
    }
    [HttpGet("my-results/export")]
    public async Task<IActionResult> ExportMyResults(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var csv = await _testingService.ExportResultsCsvAsync(startDate, endDate, null, userId);
        var fileName = $"my_results_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv; charset=utf-8", fileName);
    }
    [HttpGet("all-results/export")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ExportAllResults(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchQuery = null)
    {
        var csv = await _testingService.ExportResultsCsvAsync(startDate, endDate, searchQuery);
        var fileName = $"all_results_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csv, "text/csv; charset=utf-8", fileName);
    }
}
