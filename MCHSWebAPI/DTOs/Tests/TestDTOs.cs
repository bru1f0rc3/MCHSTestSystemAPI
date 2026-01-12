namespace MCHSWebAPI.DTOs.Tests;

public class TestDto
{
    public int Id { get; set; }
    public int? LectureId { get; set; }
    public string? LectureTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuestionsCount { get; set; }
}

public class TestDetailDto
{
    public int Id { get; set; }
    public int? LectureId { get; set; }
    public string? LectureTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public int Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Position { get; set; }
    // IsCorrect не включаем для гостей, только для админов
}

public class AnswerWithCorrectDto : AnswerDto
{
    public bool IsCorrect { get; set; }
}

public class CreateTestRequest
{
    public int? LectureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<CreateAnswerRequest> Answers { get; set; } = new();
}

public class CreateAnswerRequest
{
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Position { get; set; }
}

public class UpdateTestRequest
{
    public int? LectureId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class UpdateQuestionRequest
{
    public string? QuestionText { get; set; }
    public int? Position { get; set; }
}

public class UpdateAnswerRequest
{
    public string? AnswerText { get; set; }
    public bool? IsCorrect { get; set; }
    public int? Position { get; set; }
}
