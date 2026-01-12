namespace MCHSWebAPI.DTOs.Lectures;

public class LectureDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? VideoPath { get; set; }
    public string? DocumentPath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LectureListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool HasVideo { get; set; }
    public bool HasDocument { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLectureRequest
{
    public string Title { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? VideoPath { get; set; }
    public string? DocumentPath { get; set; }
}

public class UpdateLectureRequest
{
    public string? Title { get; set; }
    public string? TextContent { get; set; }
    public string? VideoPath { get; set; }
    public string? DocumentPath { get; set; }
}
