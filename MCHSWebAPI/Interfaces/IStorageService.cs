using MCHSWebAPI.DTOs;

namespace MCHSWebAPI.Interfaces;

public interface IStorageService
{
    /// <summary>Список файлов в подпапке хранилища по типу ("video" или "document").</summary>
    IReadOnlyList<StorageFileDto> Browse(string type);

    /// <summary>
    /// Преобразует относительный путь из хранилища в безопасный абсолютный путь на диске.
    /// Возвращает null, если путь выходит за пределы хранилища, файла нет
    /// или расширение не из числа разрешённых.
    /// </summary>
    string? ResolvePhysicalPath(string relativePath);

    /// <summary>MIME-тип для отдачи файла.</summary>
    string GetContentType(string physicalPath);

    /// <summary>
    /// Сохраняет загруженный файл в подпапку хранилища по типу ("video"/"document").
    /// Имя очищается от опасных символов, при коллизии добавляется суффикс.
    /// Возвращает null, если тип или расширение недопустимы.
    /// </summary>
    Task<StorageFileDto?> SaveAsync(string type, string originalFileName, Stream content);
}
