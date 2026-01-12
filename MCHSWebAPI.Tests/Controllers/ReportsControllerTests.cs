using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
using MCHSWebAPI.DTOs.Reports;
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
        
        // Setup admin user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "admin")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetDashboard_ReturnsDashboard()
    {
        // Arrange
        var dashboard = new DashboardDto 
        { 
            TotalUsers = 100,
            TotalTests = 50
        };
        _reportServiceMock.Setup(x => x.GetDashboardAsync())
            .ReturnsAsync(dashboard);

        // Act
        var result = await _controller.GetDashboard();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTestStatistics_ReturnsStatistics()
    {
        // Arrange
        var statistics = new List<TestStatisticsDto>
        {
            new TestStatisticsDto { TestId = 1, TestTitle = "Test 1", TotalAttempts = 50 }
        };
        _reportServiceMock.Setup(x => x.GetTestStatisticsAsync())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetTestStatistics();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsPagedReports()
    {
        // Arrange
        var reports = new List<ReportDto>
        {
            new ReportDto { Id = 1, CreatorUsername = "admin" }
        };
        var pagedResponse = new PagedResponse<ReportDto>
        {
            Items = reports,
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _reportServiceMock.Setup(x => x.GetAllAsync(1, 20))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAll(1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingReport_ReturnsReport()
    {
        // Arrange
        var report = new ReportDto { Id = 1, CreatorUsername = "admin" };
        _reportServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedReport()
    {
        // Arrange
        var request = new CreateReportRequest { DateFrom = DateOnly.FromDateTime(DateTime.Now) };
        var createdReport = new ReportDto { Id = 1, CreatorUsername = "admin" };
        _reportServiceMock.Setup(x => x.CreateAsync(It.IsAny<CreateReportRequest>(), It.IsAny<int>()))
            .ReturnsAsync(createdReport);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Delete_ExistingReport_ReturnsOk()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
