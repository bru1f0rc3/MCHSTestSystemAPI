namespace MCHSWebAPI.DTOs;

public class StartTestResponse
{
    public int TestResultId { get; set; }
    public int TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public int PassingScore { get; set; } = 70;
    public List<TestQuestionDto> Questions { get; set; } = new();
}

public class TestQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool AllowMultipleAnswers { get; set; }
    public List<TestAnswerDto> Answers { get; set; } = new();
}

public class TestAnswerDto
{
    public int AnswerId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
}

public class SubmitAnswersRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("answers")]
    public List<SubmitAnswerRequest> Answers { get; set; } = new();
}

public class FinishTestResponse
{
    public int TestResultId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public double Score { get; set; }
    public int PassingScore { get; set; } = 70;
    public string Status { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int CheatAttempts { get; set; }
    public bool AutoSubmitted { get; set; }
    public List<QuestionResultDto>? QuestionResults { get; set; }
}

public class QuestionResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? UserAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class TestResultDto
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string? LectureTitle { get; set; }
    public string? Username { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double? Score { get; set; }
    public int PassingScore { get; set; } = 70;
    public int? TimeLimitMinutes { get; set; }
    public int CheatAttempts { get; set; }
    public bool AutoSubmitted { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TestResultDetailDto : TestResultDto
{
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}

public class ReportCheatAttemptRequest
{
    public string EventType { get; set; } = "app_background";
    public string? Details { get; set; }
}
