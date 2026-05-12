using System.Text.RegularExpressions;
using MCHSWebAPI.Interfaces;
using UglyToad.PdfPig;

namespace MCHSWebAPI.Services.TestService;

public class PdfParserService : IPdfParserService
{
    public Task<ParsedTestData> ParseTestFromPdfAsync(Stream pdfStream)
    {
        var text = ExtractTextFromPdf(pdfStream);
        var questions = ParseQuestions(text);

        if (questions.Count == 0)
            throw new InvalidOperationException("Не удалось найти вопросы в PDF файле");

        return Task.FromResult(new ParsedTestData { Questions = questions });
    }

    private static string ExtractTextFromPdf(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = "";
        foreach (var page in document.GetPages())
        {
            text += page.Text + "\n";
        }
        return text;
    }

    private static List<ParsedQuestion> ParseQuestions(string text)
    {
        var questions = new List<ParsedQuestion>();
        var questionPattern = @"(\d+)\.\s*(.+?)(?=\n\s*[а-г]\)|$)";
        var questionMatches = Regex.Matches(text, questionPattern, RegexOptions.Singleline);
        var lines = text.Split('\n');
        ParsedQuestion? currentQuestion = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            var questionMatch = Regex.Match(trimmed, @"^(\d+)\.\s+(.+)");
            if (questionMatch.Success)
            {
                if (currentQuestion != null && currentQuestion.Answers.Count > 0)
                    questions.Add(currentQuestion);

                currentQuestion = new ParsedQuestion
                {
                    Text = questionMatch.Groups[2].Value.Trim()
                };
                continue;
            }
            var answerMatch = Regex.Match(trimmed, @"^([а-г])\)\s+(.+)");
            if (answerMatch.Success && currentQuestion != null)
            {
                var answerText = answerMatch.Groups[2].Value.Trim();
                var isCorrect = answerText.Contains("[true]");
                answerText = answerText.Replace("[true]", "").Trim();

                currentQuestion.Answers.Add(new ParsedAnswer
                {
                    Text = answerText,
                    IsCorrect = isCorrect
                });
            }
        }
        if (currentQuestion != null && currentQuestion.Answers.Count > 0)
            questions.Add(currentQuestion);

        return questions;
    }
}
