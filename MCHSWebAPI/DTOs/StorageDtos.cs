namespace MCHSWebAPI.DTOs;

/// <summary>
/// Описание одного файла внутри серверного хранилища.
/// Path — относительный путь вида "videos/intro.mp4", который и сохраняется в лекцию.
/// </summary>
public class StorageFileDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = string.Empty;
}
