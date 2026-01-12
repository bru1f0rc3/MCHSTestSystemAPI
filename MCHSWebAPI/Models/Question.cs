namespace MCHSWebAPI.Models;

public class Question
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int Position { get; set; }
    
    // Navigation properties
    public List<Answer>? Answers { get; set; }
}
