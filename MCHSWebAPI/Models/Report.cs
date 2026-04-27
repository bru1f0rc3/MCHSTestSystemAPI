namespace MCHSWebAPI.Models;

public class Report
{
    public int Id { get; set; }
    public int CreatedBy { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatorUsername { get; set; }
}
