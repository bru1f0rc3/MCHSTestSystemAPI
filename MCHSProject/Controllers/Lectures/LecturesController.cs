using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Lectures;
using MCHSProject.DTO.Lectures;

namespace MCHSProject.Controllers.Lectures
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturesController : ControllerBase
    {
        private readonly LectureService _lectureService;

        public LecturesController(LectureService lectureService)
        {
            _lectureService = lectureService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLectures()
        {
            var lectures = await _lectureService.GetAllLecturesAsync();
            return Ok(lectures);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLectureById(int id)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(id);
            if (lecture == null)
                return NotFound();
            
            return Ok(lecture);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLecture([FromBody] CreateLectureDTO dto)
        {
            var lecture = new Lecture
            {
                Title = dto.Title,
                TextContent = dto.TextContent,
                PathId = dto.PathId,
                CreatedAt = DateTime.UtcNow
            };
            
            var id = await _lectureService.CreateLectureAsync(lecture);
            lecture.Id = id;
            return CreatedAtAction(nameof(GetLectureById), new { id }, lecture);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLecture(int id, [FromBody] UpdateLectureDTO dto)
        {
            var lecture = new Lecture
            {
                Id = id,
                Title = dto.Title,
                TextContent = dto.TextContent,
                PathId = dto.PathId
            };
            
            var success = await _lectureService.UpdateLectureAsync(lecture);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLecture(int id)
        {
            var success = await _lectureService.DeleteLectureAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }
    }
}
