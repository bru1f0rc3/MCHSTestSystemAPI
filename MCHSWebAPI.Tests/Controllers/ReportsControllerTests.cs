using MCHSWebAPI.Controllers;
using MCHSWebAPI.Tests.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MCHSWebAPI.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _controller = new ReportsController(_reportServiceMock.Object);
        SetupUser(_controller, 1);
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk()
    {
        var dashboard = new DashboardDto { TotalUsers = 10, TotalTests = 5 };
        _reportServiceMock.Setup(s => s.GetDashboardAsync()).ReturnsAsync(dashboard);
        var result = await _controller.GetDashboard();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyStatistics_WhenExists_ReturnsOk()
    {
        var stats = new UserStatisticsDto { UserId = 1, Username = "testuser" };
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(1)).ReturnsAsync(stats);
        var result = await _controller.GetMyStatistics();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyStatistics_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(1)).ReturnsAsync((UserStatisticsDto?)null);
        var result = await _controller.GetMyStatistics();
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTestStatistics_ReturnsOk()
    {
        var stats = new List<TestStatisticsDto>
        {
            new() { TestId = 1, TestTitle = "Тест 1", AverageScore = 75 }
        };
        _reportServiceMock.Setup(s => s.GetTestStatisticsAsync()).ReturnsAsync(stats);
        var result = await _controller.GetTestStatistics();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var reports = TestDataFactory.CreatePagedResponse(new List<ReportDto>());
        _reportServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(reports);
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _reportServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        var result = await _controller.Delete(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);
        var result = await _controller.Delete(999);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
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
