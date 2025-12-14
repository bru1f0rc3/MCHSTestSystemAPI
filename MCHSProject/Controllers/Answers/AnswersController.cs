using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Answers;
using MCHSProject.DTO.Answers;

namespace MCHSProject.Controllers.Answers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnswersController : ControllerBase
    {
        private readonly AnswerService _answerService;

        public AnswersController(AnswerService answerService)
        {
            _answerService = answerService;
        }

        [HttpGet("question/{questionId}")]
        public async Task<IActionResult> GetAnswersByQuestionId(int questionId)
        {
            var answers = await _answerService.GetAnswersByQuestionIdAsync(questionId);
            return Ok(answers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnswerById(int id)
        {
            var answer = await _answerService.GetAnswerByIdAsync(id);
            if (answer == null)
                return NotFound();
            
            return Ok(answer);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnswer([FromBody] CreateAnswerDTO dto)
        {
            var answer = new Answer
            {
                QuestionId = dto.QuestionId,
                AnswerText = dto.AnswerText,
                IsCorrect = dto.IsCorrect,
                Position = dto.Position
            };
            
            var id = await _answerService.CreateAnswerAsync(answer);
            answer.Id = id;
            return CreatedAtAction(nameof(GetAnswerById), new { id }, answer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnswer(int id, [FromBody] UpdateAnswerDTO dto)
        {
            var answer = new Answer
            {
                Id = id,
                AnswerText = dto.AnswerText,
                IsCorrect = dto.IsCorrect,
                Position = dto.Position
            };
            
            var success = await _answerService.UpdateAnswerAsync(answer);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var success = await _answerService.DeleteAnswerAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpGet("{id}/check")]
        public async Task<IActionResult> CheckAnswer(int id)
        {
            var isCorrect = await _answerService.CheckAnswerIsCorrectAsync(id);
            return Ok(new { answerId = id, isCorrect });
        }
    }
}
