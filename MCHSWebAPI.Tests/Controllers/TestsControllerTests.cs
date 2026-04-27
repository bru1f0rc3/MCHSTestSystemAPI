using MCHSWebAPI.Controllers;
using MCHSWebAPI.Tests.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MCHSWebAPI.Tests.Controllers;

public class TestsControllerTests
{
    private readonly Mock<ITestService> _testServiceMock;
    private readonly TestsController _controller;

    public TestsControllerTests()
    {
        _testServiceMock = new Mock<ITestService>();
        _controller = new TestsController(_testServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var tests = new List<TestDto> { TestDataFactory.CreateTestDto() };
        var pagedResult = TestDataFactory.CreatePagedResponse(tests);
        _testServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(pagedResult);
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var test = TestDataFactory.CreateTestDto();
        _testServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(test);
        var result = await _controller.GetById(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((TestDto?)null);
        var result = await _controller.GetById(999);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = new CreateTestRequest { Title = "Тест", LectureId = 1 };
        var test = TestDataFactory.CreateTestDto();
        _testServiceMock.Setup(s => s.CreateAsync(request, 1)).ReturnsAsync(test);
        SetupUser(_controller, 1);
        var result = await _controller.Create(request);
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _testServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        var result = await _controller.Delete(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);
        var result = await _controller.Delete(999);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByLectureId_ReturnsOk()
    {
        var tests = new List<TestDto> { TestDataFactory.CreateTestDto() };
        _testServiceMock.Setup(s => s.GetByLectureIdAsync(1)).ReturnsAsync(tests);
        var result = await _controller.GetByLectureId(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    private static void SetupUser(ControllerBase controller, int userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
