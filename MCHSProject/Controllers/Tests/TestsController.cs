using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Tests;
using MCHSProject.DTO.Tests;

namespace MCHSProject.Controllers.Tests
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestsController : ControllerBase
    {
        private readonly TestService _testService;

        public TestsController(TestService testService)
        {
            _testService = testService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTests()
        {
            var tests = await _testService.GetAllTestsAsync();
            return Ok(tests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTestById(int id)
        {
            var test = await _testService.GetTestByIdAsync(id);
            if (test == null)
                return NotFound();
            
            return Ok(test);
        }

        [HttpGet("lecture/{lectureId}")]
        public async Task<IActionResult> GetTestsByLectureId(int lectureId)
        {
            var tests = await _testService.GetTestsByLectureIdAsync(lectureId);
            return Ok(tests);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTest([FromBody] CreateTestDTO dto)
        {
            var id = await _testService.CreateTestAsync(dto);
            var test = await _testService.GetTestByIdAsync(id);
            return CreatedAtAction(nameof(GetTestById), new { id }, test);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTest(int id, [FromBody] UpdateTestDTO dto)
        {
            var test = new Test
            {
                Id = id,
                Title = dto.Title,
                Description = dto.Description
            };
            
            var success = await _testService.UpdateTestAsync(test);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var success = await _testService.DeleteTestAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }
    }
}
