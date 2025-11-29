using Dapper;
using MCHSProject.ConnectionDB;
using MCHSProject.Models;

namespace MCHSProject.Services.Answers
{
    public class AnswerService
    {
        private readonly DBConnect _dbConnect;

        public AnswerService(DBConnect dbConnect)
        {
            _dbConnect = dbConnect;
        }

        public async Task<IEnumerable<Answer>> GetAnswersByQuestionIdAsync(int questionId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM answers WHERE question_id = @QuestionId ORDER BY position";
            return await connection.QueryAsync<Answer>(sql, new { QuestionId = questionId });
        }

        public async Task<Answer?> GetAnswerByIdAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT * FROM answers WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Answer>(sql, new { Id = id });
        }

        public async Task<int> CreateAnswerAsync(Answer answer)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"INSERT INTO answers (question_id, answer_text, is_correct, position) 
                       VALUES (@QuestionId, @AnswerText, @IsCorrect, @Position) 
                       RETURNING id";
            return await connection.ExecuteScalarAsync<int>(sql, answer);
        }

        public async Task<bool> UpdateAnswerAsync(Answer answer)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = @"UPDATE answers 
                       SET answer_text = @AnswerText, is_correct = @IsCorrect, position = @Position 
                       WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, answer);
            return rows > 0;
        }

        public async Task<bool> DeleteAnswerAsync(int id)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "DELETE FROM answers WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<bool> CheckAnswerIsCorrectAsync(int answerId)
        {
            using var connection = _dbConnect.CreateConnection();
            var sql = "SELECT is_correct FROM answers WHERE id = @AnswerId";
            return await connection.ExecuteScalarAsync<bool>(sql, new { AnswerId = answerId });
        }
    }
}
