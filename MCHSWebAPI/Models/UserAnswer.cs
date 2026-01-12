namespace MCHSWebAPI.Models;

public class UserAnswer
{
    public int Id { get; set; }
    public int TestResultId { get; set; }
    public int QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public DateTime AnsweredAt { get; set; }
    
    // Navigation properties
    public string? QuestionText { get; set; }
    public string? AnswerText { get; set; }
    public bool? IsCorrect { get; set; }
}
