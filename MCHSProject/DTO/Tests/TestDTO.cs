namespace MCHSProject.DTO.Tests
{
    public class CreateTestDTO
    {
        public int LectureId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
    }

    public class UpdateTestDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
