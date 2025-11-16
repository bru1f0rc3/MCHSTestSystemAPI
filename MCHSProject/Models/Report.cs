using System.Text.Json;

namespace MCHSProject.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ReportDate { get; set; }
        public JsonDocument? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
