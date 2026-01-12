namespace MCHSWebAPI.Models;

public class Test
{
    public int Id { get; set; }
    public int? LectureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public string? LectureTitle { get; set; }
    public string? CreatorUsername { get; set; }
    public List<Question>? Questions { get; set; }
}
