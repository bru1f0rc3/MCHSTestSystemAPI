using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using MCHSWebAPI.Interfaces.Services;

namespace MCHSWebAPI.Services;

public class PdfParserService : IPdfParserService
{
    public async Task<ParsedTestData> ParseTestFromPdfAsync(Stream pdfStream)
    {
        var parsedData = new ParsedTestData();

        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        ms.Position = 0;

        using var document = PdfDocument.Open(ms);

        var fullText = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            fullText.AppendLine(page.Text);
        }

        var text = fullText.ToString();

        parsedData.Questions = ParseQuestionsAndAnswers(text);

        Console.WriteLine($"=== Parsed {parsedData.Questions.Count} questions ===");

        return parsedData;
    }

    private List<ParsedQuestion> ParseQuestionsAndAnswers(string text)
    {
        var questions = new List<ParsedQuestion>();

        text = Regex.Replace(text, @"^.*?(?=\d+\.\s+)", "", RegexOptions.Singleline).Trim();
        var questionBlocks = Regex.Split(text, @"(?=\d+\.\s+)");

        foreach (var block in questionBlocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;

            var questionMatch = Regex.Match(block, @"^(\d+)\.\s+(.+?)(?=\s+[а-яёa-z]\))", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!questionMatch.Success) continue;

            var questionText = questionMatch.Groups[2].Value.Trim();
            var question = new ParsedQuestion { Text = questionText };

            var answerMatches = Regex.Matches(
                block,
                @"([а-яёa-z])\)\s+(.+?)(?=\s+[а-яёa-z]\)|$)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            foreach (Match answerMatch in answerMatches)
            {
                var answerText = answerMatch.Groups[2].Value.Trim();
                bool isCorrect = Regex.IsMatch(answerText, @"\[true\]", RegexOptions.IgnoreCase);
                answerText = Regex.Replace(answerText, @"\[true\]", "", RegexOptions.IgnoreCase).Trim();

                question.Answers.Add(new ParsedAnswer
                {
                    Text = answerText,
                    IsCorrect = isCorrect
                });
            }

            if (question.Answers.Count > 0)
            {
                questions.Add(question);
            }
        }

        return questions;
    }
}
