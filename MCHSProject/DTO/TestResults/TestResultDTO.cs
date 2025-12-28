namespace MCHSProject.DTO.TestResults
{
    public class StartTestDTO
    {
        public int UserId { get; set; }
        public int TestId { get; set; }
    }

    public class SubmitAnswerDTO
    {
        public int TestResultId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }
    }

    public class FinishTestDTO
    {
        public int ResultId { get; set; }
    }
}
