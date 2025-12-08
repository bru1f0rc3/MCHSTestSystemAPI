namespace MCHSProject.DTO.Answers
{
    public class CreateAnswerDTO
    {
        public int QuestionId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Position { get; set; }
    }

    public class UpdateAnswerDTO
    {
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Position { get; set; }
    }
}
