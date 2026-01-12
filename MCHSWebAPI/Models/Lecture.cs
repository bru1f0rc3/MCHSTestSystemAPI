namespace MCHSWebAPI.Models;

public class Lecture
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public int? PathId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public LearningPath? Path { get; set; }
}
