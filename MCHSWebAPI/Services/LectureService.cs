using MCHSWebAPI.Models;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Lectures;
using MCHSWebAPI.DTOs.Common;

namespace MCHSWebAPI.Services;

public class LectureService : ILectureService
{
    private readonly ILectureRepository _lectureRepository;

    public LectureService(ILectureRepository lectureRepository)
    {
        _lectureRepository = lectureRepository;
    }

    public async Task<LectureDto?> GetByIdAsync(int id)
    {
        var lecture = await _lectureRepository.GetByIdWithPathAsync(id);
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

    public async Task<PagedResponse<LectureListDto>> GetAllAsync(int page, int pageSize)
    {
        var lectures = await _lectureRepository.GetAllAsync(page, pageSize);
        var totalCount = await _lectureRepository.GetTotalCountAsync();

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
        var lecture = new Lecture
        {
            Title = request.Title,
            TextContent = request.TextContent
        };

        var path = new LearningPath
        {
            VideoPath = request.VideoPath,
            DocumentPath = request.DocumentPath
        };

        lecture.Id = await _lectureRepository.CreateAsync(lecture, path);
        
        return await GetByIdAsync(lecture.Id);
    }

    public async Task<bool> UpdateAsync(int id, UpdateLectureRequest request)
    {
        var lecture = await _lectureRepository.GetByIdWithPathAsync(id);
        if (lecture == null) return false;

        if (request.Title != null) lecture.Title = request.Title;
        if (request.TextContent != null) lecture.TextContent = request.TextContent;

        var path = new LearningPath
        {
            VideoPath = request.VideoPath ?? lecture.Path?.VideoPath,
            DocumentPath = request.DocumentPath ?? lecture.Path?.DocumentPath
        };

        return await _lectureRepository.UpdateAsync(lecture, path);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _lectureRepository.DeleteAsync(id);
    }
}
