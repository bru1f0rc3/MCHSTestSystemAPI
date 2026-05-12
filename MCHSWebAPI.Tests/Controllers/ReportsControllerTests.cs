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
        SetAdmin(_controller, 1);
    }

    [Fact]
    public async Task GetMyStatistics_WhenExists_ReturnsOk()
    {
        var stats = new UserStatisticsDto { UserId = 1, Username = "testuser" };
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(1)).ReturnsAsync(stats);

        var result = await _controller.GetMyStatistics();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyStatistics_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(1)).ReturnsAsync((UserStatisticsDto?)null);

        var result = await _controller.GetMyStatistics();

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserStatistics_WhenExists_ReturnsOk()
    {
        var stats = new UserStatisticsDto { UserId = 5, Username = "otheruser" };
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(5)).ReturnsAsync(stats);

        var result = await _controller.GetUserStatistics(5);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserStatistics_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetUserStatisticsAsync(999))
            .ReturnsAsync((UserStatisticsDto?)null);

        var result = await _controller.GetUserStatistics(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetOverallStatistics_ReturnsOk()
    {
        var dashboard = new DashboardDto { TotalUsers = 10, TotalTests = 5 };
        _reportServiceMock.Setup(s => s.GetDashboardAsync()).ReturnsAsync(dashboard);

        var result = await _controller.GetOverallStatistics();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTestsStatistics_ReturnsOkWithPagedData()
    {
        var stats = new List<TestStatisticsDto>
        {
            new() { TestId = 1, TestTitle = "T1", AverageScore = 70 },
            new() { TestId = 2, TestTitle = "T2", AverageScore = 80 }
        };
        _reportServiceMock.Setup(s => s.GetTestStatisticsAsync()).ReturnsAsync(stats);

        var result = await _controller.GetTestsStatistics();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUsersPerformance_ReturnsOk()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<UserPerformanceDto>
        {
            new() { UserId = 1, Username = "user1" }
        });
        _reportServiceMock.Setup(s => s.GetUsersPerformanceAsync(1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetUsersPerformance();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetailedReport_ReturnsOk()
    {
        var report = new DetailedReportDto { GeneratedAt = DateTime.UtcNow, Kind = "all" };
        _reportServiceMock.Setup(s => s.GetDetailedReportAsync(null, null, null, null))
            .ReturnsAsync(report);

        var result = await _controller.GetDetailedReport();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetailedReport_WhenServiceThrows_Returns500()
    {
        _reportServiceMock.Setup(s => s.GetDetailedReportAsync(null, null, null, null))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetDetailedReport();

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk()
    {
        var dashboard = new DashboardDto { TotalUsers = 10, TotalTests = 5 };
        _reportServiceMock.Setup(s => s.GetDashboardAsync()).ReturnsAsync(dashboard);

        var result = await _controller.GetDashboard();

        result.Result.Should().BeOfType<OkObjectResult>();
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

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<ReportDto>());
        _reportServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var report = new ReportDto { Id = 1, CreatorUsername = "admin", CreatedAt = DateTime.UtcNow };
        _reportServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(report);

        var result = await _controller.GetById(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ReportDto?)null);

        var result = await _controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = new CreateReportRequest { Title = "Отчёт", Description = "Описание" };
        var report = new ReportDto { Id = 1, CreatorUsername = "admin", CreatedAt = DateTime.UtcNow };
        _reportServiceMock.Setup(s => s.CreateAsync(request, 1)).ReturnsAsync(report);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = new CreateReportRequest { Title = "Отчёт" };
        _reportServiceMock.Setup(s => s.CreateAsync(request, 1)).ReturnsAsync((ReportDto?)null);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _reportServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static void SetAdmin(ControllerBase controller, int userId)
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
