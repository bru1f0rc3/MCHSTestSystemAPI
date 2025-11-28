using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.Questions
{
    public class QuestionService
    {
        private readonly DBConnect _dbConnect;

        public QuestionService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByTestIdAsync(int testId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM questions WHERE test_id = @TestId ORDER BY position";
            return await connection.QueryAsync<Question>(sql, new { TestId = testId });
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM questions WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Question>(sql, new { Id = id });
        }

        public async Task<int> CreateQuestionAsync(Question question)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO questions (test_id, question_text, position) 
                       VALUES (@TestId, @QuestionText, @Position) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, question);
        }

        public async Task<bool> UpdateQuestionAsync(Question question)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE questions 
                       SET question_text = @QuestionText, position = @Position 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, question);
            return rows > 0;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM questions WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task CreateQuestionsWithAnswersAsync(int testId, List<QuestionWithAnswers> questionsData)
        {
            using var connection = _dbConnect.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var questionData in questionsData)
                {
                    var questionSql = @"INSERT INTO questions (test_id, question_text, position) 
                                       VALUES (@TestId, @QuestionText, @Position) 
                                       RETURNING id";
                    var questionId = await connection.ExecuteScalarAsync<int>(questionSql, 
                        new { TestId = testId, questionData.QuestionText, questionData.Position }, transaction);

                    foreach (var answer in questionData.Answers)
                    {
                        var answerSql = @"INSERT INTO answers (question_id, answer_text, is_correct, position) 
                                         VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position)";
                        await connection.ExecuteAsync(answerSql, 
                            new { QuestionId = questionId, answer.AnswerText, answer.IsCorrect, answer.Position }, transaction);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public class QuestionWithAnswers
    {
        public string QuestionText { get; set; } = string.Empty;
        public int Position { get; set; }
        public List<AnswerData> Answers { get; set; } = new();
    }

    public class AnswerData
    {
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Position { get; set; }
    }
}
