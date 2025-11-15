namespace MCHSProject.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int Position { get; set; }
    }
}
