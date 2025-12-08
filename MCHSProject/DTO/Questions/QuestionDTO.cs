namespace MCHSProject.DTO.Questions
{
    public class CreateQuestionDTO
    {
        public int TestId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class UpdateQuestionDTO
    {
        public string QuestionText { get; set; } = string.Empty;
        public int Position { get; set; }
    }
}
