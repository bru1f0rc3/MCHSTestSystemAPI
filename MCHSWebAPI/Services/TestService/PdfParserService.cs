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
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();

        var tokenPattern = @"(?<num>\b\d+)\s*\.\s+|(?<letter>[а-гА-Г])\s*\)\s+";
        var tokens = Regex.Matches(normalized, tokenPattern);

        var questions = new List<ParsedQuestion>();
        ParsedQuestion? currentQuestion = null;
        int? expectedQuestionNumber = 1;

        for (int i = 0; i < tokens.Count; i++)
        {
            var match = tokens[i];
            var nextStart = i + 1 < tokens.Count ? tokens[i + 1].Index : normalized.Length;
            var bodyStart = match.Index + match.Length;
            var body = normalized.Substring(bodyStart, nextStart - bodyStart).Trim();

            if (match.Groups["num"].Success)
            {
                if (!int.TryParse(match.Groups["num"].Value, out var num)) continue;
                if (expectedQuestionNumber.HasValue && num != expectedQuestionNumber.Value) continue;

                if (currentQuestion != null && currentQuestion.Answers.Count > 0)
                    questions.Add(currentQuestion);

                currentQuestion = new ParsedQuestion { Text = body };
                expectedQuestionNumber = num + 1;
            }
            else if (match.Groups["letter"].Success && currentQuestion != null)
            {
                var isCorrect = body.Contains("[true]", StringComparison.OrdinalIgnoreCase);
                var answerText = Regex.Replace(body, @"\[true\]", "", RegexOptions.IgnoreCase).Trim();

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
