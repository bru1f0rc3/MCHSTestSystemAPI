using MCHSWebAPI.Tests.Helpers;

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
    public async Task GetAll_ReturnsOk()
    {
        var lectures = new List<LectureListDto>
        {
            new() { Id = 1, Title = "Лекция 1" },
            new() { Id = 2, Title = "Лекция 2" }
        };
        var paged = TestDataFactory.CreatePagedResponse(lectures);
        _lectureServiceMock.Setup(s => s.GetAllAsync(1, 20, null)).ReturnsAsync(paged);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithSearch_PassesSearchToService()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<LectureListDto>());
        _lectureServiceMock.Setup(s => s.GetAllAsync(1, 20, "fire")).ReturnsAsync(paged);

        var result = await _controller.GetAll(1, 20, "fire");

        result.Result.Should().BeOfType<OkObjectResult>();
        _lectureServiceMock.Verify(s => s.GetAllAsync(1, 20, "fire"), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var lecture = TestDataFactory.CreateLectureDto();
        _lectureServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(lecture);

        var result = await _controller.GetById(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _lectureServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((LectureDto?)null);

        var result = await _controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = TestDataFactory.CreateLectureRequest();
        var lecture = TestDataFactory.CreateLectureDto();
        _lectureServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(lecture);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = TestDataFactory.CreateLectureRequest();
        _lectureServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync((LectureDto?)null);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk()
    {
        var request = new UpdateLectureRequest { Title = "Новый заголовок" };
        _lectureServiceMock.Setup(s => s.UpdateAsync(1, request)).ReturnsAsync(true);

        var result = await _controller.Update(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        var request = new UpdateLectureRequest { Title = "Новый заголовок" };
        _lectureServiceMock.Setup(s => s.UpdateAsync(999, request)).ReturnsAsync(false);

        var result = await _controller.Update(999, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _lectureServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _lectureServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
