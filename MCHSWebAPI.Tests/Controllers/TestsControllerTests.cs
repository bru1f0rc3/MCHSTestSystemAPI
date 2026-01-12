using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
using MCHSWebAPI.DTOs.Tests;
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
        
        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "admin")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsPagedTests()
    {
        // Arrange
        var tests = new List<TestDto>
        {
            new TestDto { Id = 1, Title = "Test 1" },
            new TestDto { Id = 2, Title = "Test 2" }
        };
        var pagedResponse = new PagedResponse<TestDto>
        {
            Items = tests,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _testServiceMock.Setup(x => x.GetAllAsync(1, 20))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAll(1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingTest_ReturnsTest()
    {
        // Arrange
        var test = new TestDto { Id = 1, Title = "Test 1" };
        _testServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(test);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NonExistingTest_ReturnsNotFound()
    {
        // Arrange
        _testServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((TestDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedTest()
    {
        // Arrange
        var request = new CreateTestRequest { Title = "New Test", Description = "Test description" };
        var createdTest = new TestDto { Id = 1, Title = "New Test" };
        _testServiceMock.Setup(x => x.CreateAsync(It.IsAny<CreateTestRequest>(), It.IsAny<int>()))
            .ReturnsAsync(createdTest);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_ExistingTest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateTestRequest { Title = "Updated Test" };
        _testServiceMock.Setup(x => x.UpdateAsync(1, It.IsAny<UpdateTestRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingTest_ReturnsOk()
    {
        // Arrange
        _testServiceMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
