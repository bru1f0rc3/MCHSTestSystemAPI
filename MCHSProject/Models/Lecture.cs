namespace MCHSProject.Models
{
    public class Lecture
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public int PathId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
