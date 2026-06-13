using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.TestController;

/// <summary>
/// Контроллер для управления тестами, вопросами и ответами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestsController : AuthorizedControllerBase
{
    private readonly ITestService _testService;

    /// <summary>
    /// Создаёт контроллер и получает сервис тестов
    /// </summary>
    /// <param name="testService">Сервис для работы с тестами</param>
    public TestsController(ITestService testService)
    {
        _testService = testService;
    }

    /// <summary>
    /// Возвращает список всех тестов по страницам
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько тестов на странице (по умолчанию 20)</param>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _testService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestDto>>.Ok(result));
    }
    /// <summary>
    /// Возвращает один тест по его номеру
    /// </summary>
    /// <param name="id">Номер (id) теста</param>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TestDto>>> GetById(int id)
    {
        var result = await _testService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<TestDto>.Fail("Тест не найден"));

        return Ok(ApiResponse<TestDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает тест со всеми вопросами и ответами.
    /// Правильные ответы видны только администраторам
    /// </summary>
    /// <param name="id">Номер (id) теста</param>
    [HttpGet("{id}/full")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TestDetailDto>>> GetByIdWithQuestions(int id)
    {
        var isAdmin = User.IsInRole("admin") || User.IsInRole("superadmin");
        var result = await _testService.GetByIdWithQuestionsAsync(id, isAdmin);
        if (result == null)
            return NotFound(ApiResponse<TestDetailDto>.Fail("Тест не найден"));

        return Ok(ApiResponse<TestDetailDto>.Ok(result));
    }
    /// <summary>
    /// Возвращает все тесты, привязанные к указанной лекции
    /// </summary>
    /// <param name="lectureId">Номер (id) лекции</param>
    [HttpGet("by-lecture/{lectureId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<TestDto>>>> GetByLectureId(int lectureId)
    {
        var result = await _testService.GetByLectureIdAsync(lectureId);
        return Ok(ApiResponse<IEnumerable<TestDto>>.Ok(result));
    }
    /// <summary>
    /// Создаёт новый тест (только для админов)
    /// </summary>
    /// <param name="request">Данные нового теста с вопросами и ответами</param>
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<TestDto>>> Create([FromBody] CreateTestRequest request)
    {
        var result = await _testService.CreateAsync(request, GetUserId());
        if (result == null)
            return BadRequest(ApiResponse<TestDto>.Fail("Не удалось создать тест"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<TestDto>.Ok(result, "Тест создан"));
    }
    /// <summary>
    /// Обновляет тест по его номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) теста</param>
    /// <param name="request">Новые данные теста</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateTestRequest request)
    {
        var result = await _testService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Тест не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Тест обновлен"));
    }
    /// <summary>
    /// Удаляет тест по его номеру (только для админов)
    /// </summary>
    /// <param name="id">Номер (id) теста</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _testService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Тест не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Тест удален"));
    }
    /// <summary>
    /// Добавляет в тест новый вопрос с ответами (только для админов)
    /// </summary>
    /// <param name="testId">Номер (id) теста, в который добавляем вопрос</param>
    /// <param name="request">Данные вопроса с вариантами ответов</param>
    [HttpPost("{testId}/questions")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> AddQuestion(int testId, [FromBody] CreateQuestionRequest request)
    {
        var result = await _testService.AddQuestionAsync(testId, request);
        if (result == null)
            return BadRequest(ApiResponse<QuestionDto>.Fail("Не удалось добавить вопрос"));

        return Ok(ApiResponse<QuestionDto>.Ok(result, "Вопрос добавлен"));
    }
    /// <summary>
    /// Обновляет вопрос по его номеру (только для админов)
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса</param>
    /// <param name="request">Новые данные вопроса</param>
    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateQuestion(int questionId, [FromBody] UpdateQuestionRequest request)
    {
        var result = await _testService.UpdateQuestionAsync(questionId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Вопрос не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Вопрос обновлен"));
    }
    /// <summary>
    /// Удаляет вопрос по его номеру (только для админов)
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса</param>
    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteQuestion(int questionId)
    {
        var result = await _testService.DeleteQuestionAsync(questionId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Вопрос не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Вопрос удален"));
    }
    /// <summary>
    /// Добавляет вариант ответа к вопросу (только для админов)
    /// </summary>
    /// <param name="questionId">Номер (id) вопроса, к которому добавляем ответ</param>
    /// <param name="request">Данные ответа</param>
    [HttpPost("questions/{questionId}/answers")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<AnswerDto>>> AddAnswer(int questionId, [FromBody] CreateAnswerRequest request)
    {
        var result = await _testService.AddAnswerAsync(questionId, request);
        if (result == null)
            return BadRequest(ApiResponse<AnswerDto>.Fail("Не удалось добавить ответ"));

        return Ok(ApiResponse<AnswerDto>.Ok(result, "Ответ добавлен"));
    }
    /// <summary>
    /// Обновляет вариант ответа по его номеру (только для админов)
    /// </summary>
    /// <param name="answerId">Номер (id) ответа</param>
    /// <param name="request">Новые данные ответа</param>
    [HttpPut("answers/{answerId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAnswer(int answerId, [FromBody] UpdateAnswerRequest request)
    {
        var result = await _testService.UpdateAnswerAsync(answerId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Ответ не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ обновлен"));
    }
    /// <summary>
    /// Удаляет вариант ответа по его номеру (только для админов)
    /// </summary>
    /// <param name="answerId">Номер (id) ответа</param>
    [HttpDelete("answers/{answerId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAnswer(int answerId)
    {
        var result = await _testService.DeleteAnswerAsync(answerId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Ответ не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Ответ удален"));
    }
    /// <summary>
    /// Создаёт тест, прочитав вопросы и ответы из загруженного PDF-файла
    /// (только для админов)
    /// </summary>
    /// <param name="request">Данные импорта: PDF-файл, название, лекция и т.д.</param>
    [HttpPost("import-from-pdf")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<TestDto>>> ImportFromPdf([FromForm] ImportTestFromPdfDto request)
    {
        try
        {
            using var stream = request.PdfFile.OpenReadStream();
            var result = await _testService.ImportFromPdfAsync(
                request.LectureId, request.Title, request.Description,
                request.TimeLimitMinutes, stream, GetUserId());

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
    /// <summary>
    /// Возвращает тесты, которые текущий пользователь ещё не сдал, по страницам
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию 1)</param>
    /// <param name="pageSize">Сколько тестов на странице (по умолчанию 20)</param>
    [HttpGet("available")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResponse<TestDto>>>> GetAvailable(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _testService.GetAvailableForUserAsync(GetUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResponse<TestDto>>.Ok(result));
    }
}

