using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Questions;
using MCHSProject.DTO.Questions;

namespace MCHSProject.Controllers.Questions
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionService _questionService;

        public QuestionsController(QuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("test/{testId}")]
        public async Task<IActionResult> GetQuestionsByTestId(int testId)
        {
            var questions = await _questionService.GetQuestionsByTestIdAsync(testId);
            return Ok(questions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var question = await _questionService.GetQuestionByIdAsync(id);
            if (question == null)
                return NotFound();
            
            return Ok(question);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDTO dto)
        {
            var question = new Question
            {
                TestId = dto.TestId,
                QuestionText = dto.QuestionText,
                Position = dto.Position
            };
            
            var id = await _questionService.CreateQuestionAsync(question);
            question.Id = id;
            return CreatedAtAction(nameof(GetQuestionById), new { id }, question);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] UpdateQuestionDTO dto)
        {
            var question = new Question
            {
                Id = id,
                QuestionText = dto.QuestionText,
                Position = dto.Position
            };
            
            var success = await _questionService.UpdateQuestionAsync(question);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var success = await _questionService.DeleteQuestionAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }
    }
}
