using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using MCHSProject.Services.Questions;
using MCHSProject.Services.Tests;
using MCHSProject.DTO.Tests;
using System.Text.RegularExpressions;

namespace MCHSProject.Services.Documents
{
    public class DocumentParserService
    {
        private readonly QuestionService _questionService;
        private readonly TestService _testService;

        public DocumentParserService(QuestionService questionService, TestService testService)
        {
            _questionService = questionService;
            _testService = testService;
        }

        public async Task<int> CreateTestFromDocumentAsync(string filePath, int lectureId, string testTitle, int createdBy)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            
            List<QuestionWithAnswers> questions = extension switch
            {
                ".pdf" => ParsePdfDocument(filePath),
                ".docx" => ParseDocxDocument(filePath),
                _ => throw new NotSupportedException($"File format {extension} not supported")
            };

            var testDto = new CreateTestDTO
            {
                LectureId = lectureId,
                Title = testTitle,
                Description = $"Auto-generated from {System.IO.Path.GetFileName(filePath)}",
                CreatedBy = createdBy
            };

            var testId = await _testService.CreateTestAsync(testDto);
            await _questionService.CreateQuestionsWithAnswersAsync(testId, questions);

            return testId;
        }

        private List<QuestionWithAnswers> ParsePdfDocument(string filePath)
        {
            var questions = new List<QuestionWithAnswers>();
            
            using var pdfReader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var text = string.Empty;
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                text += PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i));
            }

            return ParseQuestionsFromText(text);
        }

        private List<QuestionWithAnswers> ParseDocxDocument(string filePath)
        {
            var text = string.Empty;
            
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document.Body;
            
            if (body != null)
            {
                text = body.InnerText;
            }

            return ParseQuestionsFromText(text);
        }

        private List<QuestionWithAnswers> ParseQuestionsFromText(string text)
        {
            var questions = new List<QuestionWithAnswers>();
            
            var questionPattern = @"(?:Вопрос|Question)\s*(\d+)[:\.\s]+(.+?)(?=(?:Вопрос|Question)\s*\d+|$)";
            var answerPattern = @"(?:^|\n)\s*([А-Яа-яA-Za-z]|\d+)\)\s*(.+?)(?=\n\s*(?:[А-Яа-яA-Za-z]|\d+)\)|$)";
            var correctPattern = @"\[(?:правильно|correct|верн)\]|\*\*|➔|✓";

            var questionMatches = Regex.Matches(text, questionPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            int position = 1;
            foreach (Match questionMatch in questionMatches)
            {
                var questionText = questionMatch.Groups[2].Value.Trim();
                var questionBlock = questionMatch.Value;

                var answers = new List<AnswerData>();
                var answerMatches = Regex.Matches(questionBlock, answerPattern, RegexOptions.Multiline);

                int answerPosition = 1;
                foreach (Match answerMatch in answerMatches)
                {
                    var answerText = answerMatch.Groups[2].Value.Trim();
                    var isCorrect = Regex.IsMatch(answerText, correctPattern, RegexOptions.IgnoreCase);
                    
                    answerText = Regex.Replace(answerText, correctPattern, "").Trim();

                    answers.Add(new AnswerData
                    {
                        AnswerText = answerText,
                        IsCorrect = isCorrect,
                        Position = answerPosition++
                    });
                }

                if (answers.Count > 0)
                {
                    questions.Add(new QuestionWithAnswers
                    {
                        QuestionText = questionText,
                        Position = position++,
                        Answers = answers
                    });
                }
            }

            return questions;
        }

        public List<QuestionWithAnswers> ParseCustomFormat(string text, string format = "numbered")
        {
            return format switch
            {
                "numbered" => ParseNumberedFormat(text),
                "bullet" => ParseBulletFormat(text),
                "json" => ParseJsonFormat(text),
                _ => ParseQuestionsFromText(text)
            };
        }

        private List<QuestionWithAnswers> ParseNumberedFormat(string text)
        {
            var questions = new List<QuestionWithAnswers>();
            var lines = text.Split('\n');
            
            QuestionWithAnswers? currentQuestion = null;
            int questionPosition = 1;
            int answerPosition = 1;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (Regex.IsMatch(trimmed, @"^\d+\.\s+"))
                {
                    if (currentQuestion != null && currentQuestion.Answers.Count > 0)
                    {
                        questions.Add(currentQuestion);
                    }

                    currentQuestion = new QuestionWithAnswers
                    {
                        QuestionText = Regex.Replace(trimmed, @"^\d+\.\s+", ""),
                        Position = questionPosition++,
                        Answers = new List<AnswerData>()
                    };
                    answerPosition = 1;
                }
                else if (currentQuestion != null && Regex.IsMatch(trimmed, @"^[а-яА-ЯA-Za-z]\)\s+"))
                {
                    var answerText = Regex.Replace(trimmed, @"^[а-яА-ЯA-Za-z]\)\s+", "");
                    var isCorrect = answerText.Contains("*") || answerText.Contains("+");
                    answerText = answerText.Replace("*", "").Replace("+", "").Trim();

                    currentQuestion.Answers.Add(new AnswerData
                    {
                        AnswerText = answerText,
                        IsCorrect = isCorrect,
                        Position = answerPosition++
                    });
                }
            }

            if (currentQuestion != null && currentQuestion.Answers.Count > 0)
            {
                questions.Add(currentQuestion);
            }

            return questions;
        }

        private List<QuestionWithAnswers> ParseBulletFormat(string text)
        {
            return ParseQuestionsFromText(text);
        }

        private List<QuestionWithAnswers> ParseJsonFormat(string text)
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<QuestionWithAnswers>>(text) ?? new List<QuestionWithAnswers>();
        }
    }
}
