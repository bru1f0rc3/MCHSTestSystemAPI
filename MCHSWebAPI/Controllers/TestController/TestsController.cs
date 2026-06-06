using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.TestController;

[ApiController]
[Route("api/[controller]")]
public class TestsController : AuthorizedControllerBase
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
            return NotFound(ApiResponse<TestDto>.Fail("РўРµСЃС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<TestDto>.Ok(result));
    }
    [HttpGet("{id}/full")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TestDetailDto>>> GetByIdWithQuestions(int id)
    {
        var isAdmin = User.IsInRole("admin") || User.IsInRole("superadmin");
        var result = await _testService.GetByIdWithQuestionsAsync(id, isAdmin);
        if (result == null)
            return NotFound(ApiResponse<TestDetailDto>.Fail("РўРµСЃС‚ РЅРµ РЅР°Р№РґРµРЅ"));

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
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<TestDto>>> Create([FromBody] CreateTestRequest request)
    {
        var result = await _testService.CreateAsync(request, GetUserId());
        if (result == null)
            return BadRequest(ApiResponse<TestDto>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ СЃРѕР·РґР°С‚СЊ С‚РµСЃС‚"));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<TestDto>.Ok(result, "РўРµСЃС‚ СЃРѕР·РґР°РЅ"));
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateTestRequest request)
    {
        var result = await _testService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("РўРµСЃС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "РўРµСЃС‚ РѕР±РЅРѕРІР»РµРЅ"));
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _testService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("РўРµСЃС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "РўРµСЃС‚ СѓРґР°Р»РµРЅ"));
    }
    [HttpPost("{testId}/questions")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> AddQuestion(int testId, [FromBody] CreateQuestionRequest request)
    {
        var result = await _testService.AddQuestionAsync(testId, request);
        if (result == null)
            return BadRequest(ApiResponse<QuestionDto>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ РґРѕР±Р°РІРёС‚СЊ РІРѕРїСЂРѕСЃ"));

        return Ok(ApiResponse<QuestionDto>.Ok(result, "Р’РѕРїСЂРѕСЃ РґРѕР±Р°РІР»РµРЅ"));
    }
    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateQuestion(int questionId, [FromBody] UpdateQuestionRequest request)
    {
        var result = await _testService.UpdateQuestionAsync(questionId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Р’РѕРїСЂРѕСЃ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "Р’РѕРїСЂРѕСЃ РѕР±РЅРѕРІР»РµРЅ"));
    }
    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteQuestion(int questionId)
    {
        var result = await _testService.DeleteQuestionAsync(questionId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Р’РѕРїСЂРѕСЃ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "Р’РѕРїСЂРѕСЃ СѓРґР°Р»РµРЅ"));
    }
    [HttpPost("questions/{questionId}/answers")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<AnswerDto>>> AddAnswer(int questionId, [FromBody] CreateAnswerRequest request)
    {
        var result = await _testService.AddAnswerAsync(questionId, request);
        if (result == null)
            return BadRequest(ApiResponse<AnswerDto>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ РґРѕР±Р°РІРёС‚СЊ РѕС‚РІРµС‚"));

        return Ok(ApiResponse<AnswerDto>.Ok(result, "РћС‚РІРµС‚ РґРѕР±Р°РІР»РµРЅ"));
    }
    [HttpPut("answers/{answerId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAnswer(int answerId, [FromBody] UpdateAnswerRequest request)
    {
        var result = await _testService.UpdateAnswerAsync(answerId, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("РћС‚РІРµС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "РћС‚РІРµС‚ РѕР±РЅРѕРІР»РµРЅ"));
    }
    [HttpDelete("answers/{answerId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAnswer(int answerId)
    {
        var result = await _testService.DeleteAnswerAsync(answerId);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("РћС‚РІРµС‚ РЅРµ РЅР°Р№РґРµРЅ"));

        return Ok(ApiResponse<bool>.Ok(true, "РћС‚РІРµС‚ СѓРґР°Р»РµРЅ"));
    }
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
                return BadRequest(ApiResponse<TestDto>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ РёРјРїРѕСЂС‚РёСЂРѕРІР°С‚СЊ С‚РµСЃС‚ РёР· PDF"));

            return Ok(ApiResponse<TestDto>.Ok(result, "РўРµСЃС‚ СѓСЃРїРµС€РЅРѕ РёРјРїРѕСЂС‚РёСЂРѕРІР°РЅ РёР· PDF"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TestDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TestDto>.Fail($"РћС€РёР±РєР° РїСЂРё РёРјРїРѕСЂС‚Рµ: {ex.Message}"));
        }
    }
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

