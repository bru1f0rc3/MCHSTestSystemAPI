using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.TestResults;
using MCHSProject.DTO.TestResults;

namespace MCHSProject.Controllers.TestResults
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestResultsController : ControllerBase
    {
        private readonly TestResultService _testResultService;

        public TestResultsController(TestResultService testResultService)
        {
            _testResultService = testResultService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetResultsByUserId(int userId)
        {
            var results = await _testResultService.GetResultsByUserIdAsync(userId);
            return Ok(results);
        }

        [HttpGet("test/{testId}")]
        public async Task<IActionResult> GetResultsByTestId(int testId)
        {
            var results = await _testResultService.GetResultsByTestIdAsync(testId);
            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetResultById(int id)
        {
            var result = await _testResultService.GetResultByIdAsync(id);
            if (result == null)
                return NotFound();
            
            return Ok(result);
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartTest([FromBody] StartTestDTO dto)
        {
            var resultId = await _testResultService.StartTestAsync(dto.UserId, dto.TestId);
            return Ok(new { testResultId = resultId });
        }

        [HttpPost("answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerDTO dto)
        {
            var userAnswer = new UserAnswer
            {
                TestResultId = dto.TestResultId,
                QuestionId = dto.QuestionId,
                AnswerId = dto.AnswerId,
                AnsweredAt = DateTime.UtcNow
            };
            
            var success = await _testResultService.SaveUserAnswerAsync(userAnswer);
            return Ok(new { success });
        }

        [HttpPost("finish")]
        public async Task<IActionResult> FinishTest([FromBody] FinishTestDTO dto)
        {
            var score = await _testResultService.CalculateTestScoreAsync(dto.ResultId);
            var success = await _testResultService.FinishTestAsync(dto.ResultId, score);
            
            return Ok(new { success, score });
        }

        [HttpGet("{resultId}/answers")]
        public async Task<IActionResult> GetUserAnswers(int resultId)
        {
            var answers = await _testResultService.GetUserAnswersByResultIdAsync(resultId);
            return Ok(answers);
        }
    }
}
