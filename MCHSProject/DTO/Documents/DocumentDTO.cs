namespace MCHSProject.DTO.Documents
{
    public class CreateTestFromDocumentDTO
    {
        public int LectureId { get; set; }
        public string TestTitle { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public IFormFile DocumentFile { get; set; } = null!;
    }
}
