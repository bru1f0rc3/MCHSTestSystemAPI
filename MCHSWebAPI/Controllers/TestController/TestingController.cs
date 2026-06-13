using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.TestController;

/// <summary>
/// Контроллер прохождения тестов: старт, ответы, завершение и результаты
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestingController : AuthorizedControllerBase
{
    private readonly ITestingService _testingService;

    /// <summary>
    /// Создаёт контроллер и получает сервис прохождения тестов
    /// </summary>
    /// <param name="testingService">Сервис для прохождения тестов</param>
    public TestingController(ITestingService testingService)
    {
        _testingService = testingService;
    }

    /// <summary>
    /// Начинает прохождение теста текущим пользователем
    /// </summary>
    /// <param name="testId">Номер (id) теста, который начинают</param>
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
    /// Сохраняет один ответ на вопрос во время прохождения теста
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
    /// <param name="request">Данные ответа: номер вопроса и номер выбранного ответа</param>
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
    /// <summary>
    /// Сохраняет сразу несколько ответов во время прохождения теста
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
    /// <param name="request">Список ответов на вопросы</param>
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
    /// <summary>
    /// Завершает тест и возвращает итоговый результат
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
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
    /// Фиксирует попытку списывания во время прохождения теста
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
    /// <param name="request">Данные о нарушении (можно не указывать)</param>
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
    /// <summary>
    /// Возвращает краткий результат прохождения теста
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
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
    /// Возвращает подробный результат теста с разбором по каждому вопросу
    /// </summary>
    /// <param name="testResultId">Номер попытки прохождения теста</param>
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
    /// Возвращает результаты тестов текущего пользователя по страницам
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько результатов на странице (по умолчанию 20)</param>
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
    /// Возвращает результаты всех пользователей по страницам
    /// с фильтром по датам и поиском (только для админов)
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько результатов на странице (по умолчанию 20)</param>
    /// <param name="startDate">Начало периода (можно не указывать)</param>
    /// <param name="endDate">Конец периода (можно не указывать)</param>
    /// <param name="searchQuery">Текст для поиска по логину или названию теста (можно не указывать)</param>
    [HttpGet("all-results")]
    [Authorize(Roles = "admin,superadmin")]
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
}
