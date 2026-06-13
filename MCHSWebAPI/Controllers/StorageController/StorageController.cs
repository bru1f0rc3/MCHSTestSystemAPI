using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.StorageController;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storage;

    public StorageController(IStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Просмотр серверной папки хранилища. Только админам.
    /// type: "video" — список видео, "document" — список PDF.
    /// </summary>
    [HttpGet("browse")]
    [Authorize(Roles = "admin,superadmin")]
    public ActionResult<ApiResponse<List<StorageFileDto>>> Browse([FromQuery] string type = "video")
    {
        var files = _storage.Browse(type).ToList();
        return Ok(ApiResponse<List<StorageFileDto>>.Ok(files));
    }

    /// <summary>
    /// Загрузка файла в серверное хранилище с устройства. Только админам.
    /// type: "video" — в папку videos, "document" — в documents.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "admin,superadmin")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponse<StorageFileDto>>> Upload(
        [FromQuery] string type,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<StorageFileDto>.Fail("Файл не передан"));

        await using var stream = file.OpenReadStream();
        var saved = await _storage.SaveAsync(type, file.FileName, stream);
        if (saved == null)
            return BadRequest(ApiResponse<StorageFileDto>.Fail("Недопустимый тип или расширение файла"));

        return Ok(ApiResponse<StorageFileDto>.Ok(saved, "Файл загружен"));
    }

    /// <summary>
    /// Отдаёт файл из хранилища по относительному пути с поддержкой Range
    /// (нужно для перемотки видео встроенным плеером).
    /// </summary>
    [HttpGet("file")]
    [AllowAnonymous]
    public IActionResult GetFile([FromQuery] string path)
    {
        var physical = _storage.ResolvePhysicalPath(path);
        if (physical == null)
            return NotFound();

        var contentType = _storage.GetContentType(physical);
        return PhysicalFile(physical, contentType, enableRangeProcessing: true);
    }
}
