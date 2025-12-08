namespace MCHSProject.DTO.Reports
{
    public class CreateReportDTO
    {
        public int CreatedBy { get; set; }
        public DateTime ReportDate { get; set; }
        public object Content { get; set; } = new { };
    }
}
