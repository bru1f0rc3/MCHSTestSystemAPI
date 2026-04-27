using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Controllers.FileController;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private const long MaxVideoSize = 5L * 1024 * 1024 * 1024;

    public FilesController(IWebHostEnvironment env)
    {
        _env = env;
    }
    [HttpPost("upload-video")]
    [Authorize(Roles = "admin")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<FileUploadResult>>> UploadVideo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<FileUploadResult>.Fail("Файл не указан"));

        if (file.Length > MaxVideoSize)
            return BadRequest(ApiResponse<FileUploadResult>.Fail("Размер видео не может превышать 5 ГБ"));

        var allowedExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm", ".flv" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<FileUploadResult>.Fail("Недопустимый формат видео. Допустимые: " + string.Join(", ", allowedExtensions)));

        var result = await SaveFile(file, "videos");
        return Ok(ApiResponse<FileUploadResult>.Ok(result, "Видео загружено"));
    }
    [HttpPost("upload-document")]
    [Authorize(Roles = "admin")]
    [RequestSizeLimit(long.MaxValue)]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<ActionResult<ApiResponse<FileUploadResult>>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<FileUploadResult>.Fail("Файл не указан"));

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<FileUploadResult>.Fail("Недопустимый формат документа. Допустимые: " + string.Join(", ", allowedExtensions)));

        var result = await SaveFile(file, "documents");
        return Ok(ApiResponse<FileUploadResult>.Ok(result, "Документ загружен"));
    }

    private async Task<FileUploadResult> SaveFile(IFormFile file, string subfolder)
    {
        var storagePath = Path.Combine(_env.ContentRootPath, "storage", subfolder);
        if (!Directory.Exists(storagePath))
            Directory.CreateDirectory(storagePath);

        var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var filePath = Path.Combine(storagePath, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new FileUploadResult
        {
            FilePath = $"/storage/{subfolder}/{uniqueName}",
            OriginalName = file.FileName,
            Size = file.Length
        };
    }
}
