namespace MCHSProject.Models
{
    public class UserAnswer
    {
        public int Id { get; set; }
        public int TestResultId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }
        public DateTime AnsweredAt { get; set; }
    }
}
