namespace MCHSProject.DTO.Lectures
{
    public class CreateLectureDTO
    {
        public string Title { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public int PathId { get; set; }
    }

    public class UpdateLectureDTO
    {
        public string Title { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public int PathId { get; set; }
    }
}
