using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using Microsoft.AspNetCore.StaticFiles;

namespace MCHSWebAPI.Services.StorageService;

/// <summary>
/// Доступ к локальному файловому хранилищу сервера.
/// Видео лежат в {root}/videos, документы — в {root}/documents.
/// Наружу отдаются только файлы внутри корня и только с разрешёнными расширениями.
/// </summary>
public class StorageService : IStorageService
{
    private static readonly string[] VideoExtensions =
        { ".mp4", ".webm", ".m4v", ".mov", ".mkv", ".ogg", ".ogv", ".avi" };
    private static readonly string[] DocumentExtensions = { ".pdf" };

    private const string VideosFolder = "videos";
    private const string DocumentsFolder = "documents";

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly string _rootFull;

    public StorageService(IConfiguration config, IWebHostEnvironment env)
    {
        var configured = config["Storage:RootPath"];
        if (string.IsNullOrWhiteSpace(configured))
            configured = "Storage";

        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);

        _rootFull = Path.GetFullPath(root);

        // Гарантируем наличие папок хранилища при старте.
        Directory.CreateDirectory(Path.Combine(_rootFull, VideosFolder));
        Directory.CreateDirectory(Path.Combine(_rootFull, DocumentsFolder));
    }

    public IReadOnlyList<StorageFileDto> Browse(string type)
    {
        var category = Categorize(type);
        if (category == null)
            return Array.Empty<StorageFileDto>();

        var (folder, extensions) = category.Value;
        var dir = Path.Combine(_rootFull, folder);
        if (!Directory.Exists(dir))
            return Array.Empty<StorageFileDto>();

        return Directory.EnumerateFiles(dir)
            .Select(f => new FileInfo(f))
            .Where(fi => extensions.Contains(fi.Extension.ToLowerInvariant()))
            .OrderBy(fi => fi.Name, StringComparer.OrdinalIgnoreCase)
            .Select(fi => new StorageFileDto
            {
                Name = fi.Name,
                Path = $"{folder}/{fi.Name}",
                SizeBytes = fi.Length,
                Extension = fi.Extension.TrimStart('.').ToLowerInvariant()
            })
            .ToList();
    }

    public string? ResolvePhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        // Нормализуем разделители и срезаем ведущие слэши, чтобы Combine не воспринял путь как абсолютный.
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');

        var candidate = Path.GetFullPath(Path.Combine(_rootFull, normalized));

        // Защита от traversal: итоговый путь обязан лежать внутри корня хранилища.
        var rootWithSep = _rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? _rootFull
            : _rootFull + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(candidate))
            return null;

        var ext = Path.GetExtension(candidate).ToLowerInvariant();
        if (!VideoExtensions.Contains(ext) && !DocumentExtensions.Contains(ext))
            return null;

        return candidate;
    }

    public string GetContentType(string physicalPath)
    {
        return ContentTypeProvider.TryGetContentType(physicalPath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    public async Task<StorageFileDto?> SaveAsync(string type, string originalFileName, Stream content)
    {
        var category = Categorize(type);
        if (category == null)
            return null;

        var (folder, extensions) = category.Value;

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !extensions.Contains(ext))
            return null;

        var dir = Path.Combine(_rootFull, folder);
        Directory.CreateDirectory(dir);

        var safeName = MakeSafeFileName(originalFileName, ext);
        var fullPath = EnsureUnique(Path.Combine(dir, safeName));

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fs);
        }

        var fi = new FileInfo(fullPath);
        return new StorageFileDto
        {
            Name = fi.Name,
            Path = $"{folder}/{fi.Name}",
            SizeBytes = fi.Length,
            Extension = ext.TrimStart('.')
        };
    }

    private static string MakeSafeFileName(string original, string ext)
    {
        var baseName = Path.GetFileNameWithoutExtension(original);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(baseName.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "file";
        return cleaned + ext;
    }

    private static string EnsureUnique(string fullPath)
    {
        if (!File.Exists(fullPath))
            return fullPath;

        var dir = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var ext = Path.GetExtension(fullPath);
        for (var i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name}_{i}{ext}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private static (string folder, string[] extensions)? Categorize(string type) =>
        type?.ToLowerInvariant() switch
        {
            "video" => (VideosFolder, VideoExtensions),
            "document" => (DocumentsFolder, DocumentExtensions),
            _ => null
        };
}
