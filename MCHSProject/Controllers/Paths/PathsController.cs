using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Paths;
using MCHSProject.DTO.Paths;

namespace MCHSProject.Controllers.Paths
{
    [ApiController]
    [Route("api/[controller]")]
    public class PathsController : ControllerBase
    {
        private readonly PathService _pathService;

        public PathsController(PathService pathService)
        {
            _pathService = pathService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPathById(int id)
        {
            var path = await _pathService.GetPathByIdAsync(id);
            if (path == null)
                return NotFound();
            
            return Ok(path);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePath([FromBody] PathDTO dto)
        {
            var id = await _pathService.CreatePathAsync(dto.VideoPath, dto.DocumentPath);
            return CreatedAtAction(nameof(GetPathById), new { id }, new { id, dto.VideoPath, dto.DocumentPath });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePath(int id, [FromBody] PathDTO dto)
        {
            var success = await _pathService.UpdatePathAsync(id, dto.VideoPath, dto.DocumentPath);
            if (!success)
                return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePath(int id)
        {
            var success = await _pathService.DeletePathAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }
    }
}
