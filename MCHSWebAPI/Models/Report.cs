namespace MCHSWebAPI.Models;

public class Report
{
    public int Id { get; set; }
    public int CreatedBy { get; set; }
    public DateOnly ReportDate { get; set; }
    public string? Content { get; set; } // JSON
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public string? CreatorUsername { get; set; }
}
