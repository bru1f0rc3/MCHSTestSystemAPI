using Microsoft.AspNetCore.Mvc;
using MCHSProject.Services.Reports;
using MCHSProject.DTO.Reports;

namespace MCHSProject.Controllers.Reports
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();
            
            return Ok(report);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetReportsByUserId(int userId)
        {
            var reports = await _reportService.GetReportsByUserIdAsync(userId);
            return Ok(reports);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDTO dto)
        {
            var id = await _reportService.CreateReportAsync(dto.CreatedBy, dto.ReportDate, dto.Content);
            return CreatedAtAction(nameof(GetReportById), new { id }, new { id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var success = await _reportService.DeleteReportAsync(id);
            if (!success)
                return NotFound();
            
            return NoContent();
        }
    }
}
