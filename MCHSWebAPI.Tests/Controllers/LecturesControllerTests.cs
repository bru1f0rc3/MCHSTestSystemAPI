using MCHSWebAPI.Controllers;
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
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var lectures = new List<LectureListDto>
        {
            new() { Id = 1, Title = "Лекция 1" },
            new() { Id = 2, Title = "Лекция 2" }
        };
        var pagedResult = TestDataFactory.CreatePagedResponse(lectures);
        _lectureServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(pagedResult);
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var lecture = TestDataFactory.CreateLectureDto();
        _lectureServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(lecture);
        var result = await _controller.GetById(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _lectureServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((LectureDto?)null);
        var result = await _controller.GetById(999);
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = TestDataFactory.CreateLectureRequest();
        var lecture = TestDataFactory.CreateLectureDto();
        _lectureServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(lecture);
        var result = await _controller.Create(request);
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _lectureServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        var result = await _controller.Delete(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _lectureServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);
        var result = await _controller.Delete(999);
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }
}
