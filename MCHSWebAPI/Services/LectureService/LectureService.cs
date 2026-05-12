using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.LectureService;

public class LectureService(IDbConnectionFactory db) : ILectureService
{
    public async Task<LectureDto?> GetByIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = @"
            SELECT l.id, l.title, l.text_content as TextContent, l.path_id as PathId, l.created_at as CreatedAt,
                   p.id, p.video_path as VideoPath, p.document_path as DocumentPath
            FROM lectures l
            LEFT JOIN paths p ON l.path_id = p.id
            WHERE l.id = @Id";

        var result = await connection.QueryAsync<Lecture, LearningPath?, Lecture>(
            sql,
            (lecture, path) => { lecture.Path = path; return lecture; },
            new { Id = id },
            splitOn: "id");

        var lecture = result.FirstOrDefault();
        if (lecture == null) return null;

        return new LectureDto
        {
            Id = lecture.Id,
            Title = lecture.Title,
            TextContent = lecture.TextContent,
            VideoPath = lecture.Path?.VideoPath,
            DocumentPath = lecture.Path?.DocumentPath,
            CreatedAt = lecture.CreatedAt
        };
    }

    public async Task<PagedResponse<LectureListDto>> GetAllAsync(int page, int pageSize, string? search = null)
    {
        using var connection = db.CreateConnection();

        var buildWhere = "";
        var queryParams = new DynamicParameters();
        queryParams.Add("PageSize", pageSize);
        queryParams.Add("Offset", (page - 1) * pageSize);

        if (!string.IsNullOrWhiteSpace(search))
        {
            buildWhere = "WHERE l.title ILIKE @Search OR l.text_content ILIKE @Search";
            queryParams.Add("Search", $"%{search}%");
        }

        var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM lectures l {buildWhere}", queryParams);

        string sql = $@"
            SELECT l.id, l.title, l.text_content as TextContent, l.path_id as PathId, l.created_at as CreatedAt,
                   p.id, p.video_path as VideoPath, p.document_path as DocumentPath
            FROM lectures l
            LEFT JOIN paths p ON l.path_id = p.id
            {buildWhere}
            ORDER BY l.created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var lectures = await connection.QueryAsync<Lecture, LearningPath?, Lecture>(
            sql,
            (lecture, path) => { lecture.Path = path; return lecture; },
            queryParams,
            splitOn: "id");

        return new PagedResponse<LectureListDto>
        {
            Items = lectures.Select(l => new LectureListDto
            {
                Id = l.Id,
                Title = l.Title,
                HasVideo = !string.IsNullOrEmpty(l.Path?.VideoPath),
                HasDocument = !string.IsNullOrEmpty(l.Path?.DocumentPath),
                CreatedAt = l.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LectureDto?> CreateAsync(CreateLectureRequest request)
    {
        using var connection = db.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int? pathId = null;
            if (!string.IsNullOrEmpty(request.VideoPath) || !string.IsNullOrEmpty(request.DocumentPath))
            {
                pathId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO paths (video_path, document_path) VALUES (@VideoPath, @DocumentPath) RETURNING id",
                    new { request.VideoPath, request.DocumentPath }, transaction);
            }

            var lectureId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO lectures (title, text_content, path_id)
                  VALUES (@Title, @TextContent, @PathId) RETURNING id",
                new { request.Title, request.TextContent, PathId = pathId }, transaction);

            transaction.Commit();
            return await GetByIdAsync(lectureId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateLectureRequest request)
    {
        using var connection = db.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var current = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, title, text_content as TextContent, path_id as PathId FROM lectures WHERE id = @Id",
                new { Id = id }, transaction);

            if (current == null) return false;

            int? pathId = (int?)current.PathId;

            if (request.VideoPath != null || request.DocumentPath != null)
            {
                if (pathId.HasValue)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE paths SET video_path = COALESCE(@VideoPath, video_path),
                                           document_path = COALESCE(@DocumentPath, document_path) WHERE id = @Id",
                        new { request.VideoPath, request.DocumentPath, Id = pathId.Value }, transaction);
                }
                else
                {
                    pathId = await connection.ExecuteScalarAsync<int>(
                        @"INSERT INTO paths (video_path, document_path) VALUES (@VideoPath, @DocumentPath) RETURNING id",
                        new { request.VideoPath, request.DocumentPath }, transaction);
                }
            }

            var affected = await connection.ExecuteAsync(
                @"UPDATE lectures SET title = COALESCE(@Title, title),
                                      text_content = COALESCE(@TextContent, text_content),
                                      path_id = @PathId WHERE id = @Id",
                new { request.Title, request.TextContent, PathId = pathId, Id = id }, transaction);

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
        using var connection = db.CreateConnection();
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
