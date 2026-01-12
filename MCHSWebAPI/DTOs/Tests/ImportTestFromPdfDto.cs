namespace MCHSWebAPI.DTOs.Tests;

public class ImportTestFromPdfDto
{
    public int LectureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile PdfFile { get; set; } = null!;
}
