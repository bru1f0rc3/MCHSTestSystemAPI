using Dapper;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Repositories;

public class LectureRepository : ILectureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LectureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Lecture?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Lecture>(
            @"SELECT id, title, text_content as TextContent, path_id as PathId, created_at as CreatedAt 
              FROM lectures WHERE id = @Id", new { Id = id });
    }

    public async Task<Lecture?> GetByIdWithPathAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT l.id, l.title, l.text_content as TextContent, l.path_id as PathId, l.created_at as CreatedAt,
                   p.id, p.video_path as VideoPath, p.document_path as DocumentPath
            FROM lectures l
            LEFT JOIN paths p ON l.path_id = p.id
            WHERE l.id = @Id";

        var result = await connection.QueryAsync<Lecture, LearningPath?, Lecture>(
            sql,
            (lecture, path) =>
            {
                lecture.Path = path;
                return lecture;
            },
            new { Id = id },
            splitOn: "id");

        return result.FirstOrDefault();
    }

    public async Task<IEnumerable<Lecture>> GetAllAsync(int page, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT l.id, l.title, l.text_content as TextContent, l.path_id as PathId, l.created_at as CreatedAt,
                   p.id, p.video_path as VideoPath, p.document_path as DocumentPath
            FROM lectures l
            LEFT JOIN paths p ON l.path_id = p.id
            ORDER BY l.created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var result = await connection.QueryAsync<Lecture, LearningPath?, Lecture>(
            sql,
            (lecture, path) =>
            {
                lecture.Path = path;
                return lecture;
            },
            new { PageSize = pageSize, Offset = (page - 1) * pageSize },
            splitOn: "id");

        return result;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lectures");
    }

    public async Task<int> CreateAsync(Lecture lecture, LearningPath? path)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int? pathId = null;
            if (path != null && (!string.IsNullOrEmpty(path.VideoPath) || !string.IsNullOrEmpty(path.DocumentPath)))
            {
                pathId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO paths (video_path, document_path) VALUES (@VideoPath, @DocumentPath) RETURNING id",
                    path, transaction);
            }

            var lectureId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO lectures (title, text_content, path_id) 
                  VALUES (@Title, @TextContent, @PathId) RETURNING id",
                new { lecture.Title, lecture.TextContent, PathId = pathId }, transaction);

            transaction.Commit();
            return lectureId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Lecture lecture, LearningPath? path)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var currentLecture = await connection.QueryFirstOrDefaultAsync<Lecture>(
                "SELECT path_id as PathId FROM lectures WHERE id = @Id", 
                new { Id = lecture.Id }, transaction);

            if (currentLecture == null) return false;

            int? pathId = currentLecture.PathId;

            if (path != null)
            {
                if (pathId.HasValue)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE paths SET video_path = @VideoPath, document_path = @DocumentPath WHERE id = @Id",
                        new { path.VideoPath, path.DocumentPath, Id = pathId.Value }, transaction);
                }
                else if (!string.IsNullOrEmpty(path.VideoPath) || !string.IsNullOrEmpty(path.DocumentPath))
                {
                    pathId = await connection.ExecuteScalarAsync<int>(
                        @"INSERT INTO paths (video_path, document_path) VALUES (@VideoPath, @DocumentPath) RETURNING id",
                        path, transaction);
                }
            }

            var affected = await connection.ExecuteAsync(
                @"UPDATE lectures SET title = @Title, text_content = @TextContent, path_id = @PathId WHERE id = @Id",
                new { lecture.Title, lecture.TextContent, PathId = pathId, lecture.Id }, transaction);

            transaction.Commit();
            return affected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var pathId = await connection.ExecuteScalarAsync<int?>(
                "SELECT path_id FROM lectures WHERE id = @Id", new { Id = id }, transaction);

            var affected = await connection.ExecuteAsync(
                "DELETE FROM lectures WHERE id = @Id", new { Id = id }, transaction);

            if (pathId.HasValue)
            {
                await connection.ExecuteAsync(
                    "DELETE FROM paths WHERE id = @Id", new { Id = pathId.Value }, transaction);
            }

            transaction.Commit();
            return affected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

