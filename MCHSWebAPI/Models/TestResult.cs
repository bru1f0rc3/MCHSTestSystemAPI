namespace MCHSWebAPI.Models;

public class TestResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TestId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double? Score { get; set; }
    
    // Navigation properties
    public string? Username { get; set; }
    public string? TestTitle { get; set; }
    public string? Status { get; set; }
}
