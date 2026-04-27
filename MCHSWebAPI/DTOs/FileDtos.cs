namespace MCHSWebAPI.DTOs;

public class FileUploadResult
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public long Size { get; set; }
}
