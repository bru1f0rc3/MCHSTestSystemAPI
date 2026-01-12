using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
using MCHSWebAPI.DTOs.Lectures;

namespace MCHSWebAPI.Tests.Controllers;

public class LecturesControllerTests
{
    private readonly Mock<ILectureService> _lectureServiceMock;
    private readonly LecturesController _controller;

    public LecturesControllerTests()
    {
        _lectureServiceMock = new Mock<ILectureService>();
        _controller = new LecturesController(_lectureServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedLectures()
    {
        // Arrange
        var lectures = new List<LectureListDto>
        {
            new LectureListDto { Id = 1, Title = "Lecture 1" },
            new LectureListDto { Id = 2, Title = "Lecture 2" }
        };
        var pagedResponse = new PagedResponse<LectureListDto>
        {
            Items = lectures,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _lectureServiceMock.Setup(x => x.GetAllAsync(1, 20))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAll(1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingLecture_ReturnsLecture()
    {
        // Arrange
        var lecture = new LectureDto { Id = 1, Title = "Test Lecture" };
        _lectureServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(lecture);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NonExistingLecture_ReturnsNotFound()
    {
        // Arrange
        _lectureServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((LectureDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedLecture()
    {
        // Arrange
        var request = new CreateLectureRequest { Title = "New Lecture" };
        var createdLecture = new LectureDto { Id = 1, Title = "New Lecture" };
        _lectureServiceMock.Setup(x => x.CreateAsync(It.IsAny<CreateLectureRequest>()))
            .ReturnsAsync(createdLecture);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_ExistingLecture_ReturnsOk()
    {
        // Arrange
        var request = new UpdateLectureRequest { Title = "Updated Lecture" };
        _lectureServiceMock.Setup(x => x.UpdateAsync(1, It.IsAny<UpdateLectureRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingLecture_ReturnsOk()
    {
        // Arrange
        _lectureServiceMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
