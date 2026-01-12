using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Services;

public interface IPdfParserService
{
    Task<ParsedTestData> ParseTestFromPdfAsync(Stream pdfStream);
}

public class ParsedTestData
{
    public List<ParsedQuestion> Questions { get; set; } = new();
}

public class ParsedQuestion
{
    public string Text { get; set; } = string.Empty;
    public List<ParsedAnswer> Answers { get; set; } = new();
}

public class ParsedAnswer
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
