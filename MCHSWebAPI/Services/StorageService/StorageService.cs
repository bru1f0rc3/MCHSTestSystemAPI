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

    /// <summary>
    /// Настраивает хранилище: определяет корневую папку
    /// и создаёт папки для видео и документов, если их ещё нет
    /// </summary>
    /// <param name="config">Настройки приложения (откуда берётся путь к хранилищу)</param>
    /// <param name="env">Сведения об окружении приложения (нужны для пути к папке)</param>
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

    /// <summary>
    /// Возвращает список файлов в хранилище нужного типа (видео или документы)
    /// </summary>
    /// <param name="type">Тип файлов: "video" или "document"</param>
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

    /// <summary>
    /// Превращает относительный путь в реальный путь к файлу на диске.
    /// Проверяет, что файл лежит внутри хранилища и имеет разрешённое расширение,
    /// иначе возвращает null
    /// </summary>
    /// <param name="relativePath">Относительный путь к файлу внутри хранилища</param>
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

    /// <summary>
    /// Определяет тип содержимого файла (MIME-тип) по его пути.
    /// Если определить не удалось — возвращает универсальный тип
    /// </summary>
    /// <param name="physicalPath">Путь к файлу на диске</param>
    public string GetContentType(string physicalPath)
    {
        return ContentTypeProvider.TryGetContentType(physicalPath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    /// <summary>
    /// Сохраняет загруженный файл в нужную папку хранилища.
    /// Делает имя безопасным и добавляет число, если такой файл уже есть
    /// </summary>
    /// <param name="type">Тип файла: "video" или "document"</param>
    /// <param name="originalFileName">Исходное имя загруженного файла</param>
    /// <param name="content">Поток с содержимым файла</param>
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

    /// <summary>
    /// Делает имя файла безопасным: убирает запрещённые символы.
    /// Если имя оказалось пустым — использует "file"
    /// </summary>
    /// <param name="original">Исходное имя файла</param>
    /// <param name="ext">Расширение файла (например, ".pdf")</param>
    private static string MakeSafeFileName(string original, string ext)
    {
        var baseName = Path.GetFileNameWithoutExtension(original);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(baseName.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "file";
        return cleaned + ext;
    }

    /// <summary>
    /// Подбирает уникальный путь к файлу: если такой уже есть,
    /// добавляет к имени число (_1, _2 и т.д.)
    /// </summary>
    /// <param name="fullPath">Желаемый полный путь к файлу</param>
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

    /// <summary>
    /// По типу файла определяет папку и список разрешённых расширений.
    /// Для неизвестного типа возвращает null
    /// </summary>
    /// <param name="type">Тип файла: "video" или "document"</param>
    private static (string folder, string[] extensions)? Categorize(string type) =>
        type?.ToLowerInvariant() switch
        {
            "video" => (VideosFolder, VideoExtensions),
            "document" => (DocumentsFolder, DocumentExtensions),
            _ => null
        };
}
