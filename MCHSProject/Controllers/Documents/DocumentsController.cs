using Microsoft.AspNetCore.Mvc;
using MCHSProject.Services.Documents;
using MCHSProject.DTO.Documents;

namespace MCHSProject.Controllers.Documents
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentParserService _documentParser;

        public DocumentsController(DocumentParserService documentParser)
        {
            _documentParser = documentParser;
        }

        [HttpPost("create-test")]
        public async Task<IActionResult> CreateTestFromDocument([FromForm] CreateTestFromDocumentDTO dto)
        {
            if (dto.DocumentFile == null || dto.DocumentFile.Length == 0)
                return BadRequest("File is required");

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), dto.DocumentFile.FileName);
            
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await dto.DocumentFile.CopyToAsync(stream);
                }

                var testId = await _documentParser.CreateTestFromDocumentAsync(
                    tempPath,
                    dto.LectureId,
                    dto.TestTitle,
                    dto.CreatedBy
                );

                return Ok(new { testId, message = "Test created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
    }
}
