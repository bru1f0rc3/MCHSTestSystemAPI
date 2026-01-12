using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
using MCHSWebAPI.DTOs.Testing;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MCHSWebAPI.Tests.Controllers;

public class TestingControllerTests
{
    private readonly Mock<ITestingService> _testingServiceMock;
    private readonly TestingController _controller;

    public TestingControllerTests()
    {
        _testingServiceMock = new Mock<ITestingService>();
        _controller = new TestingController(_testingServiceMock.Object);
        
        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task StartTest_ValidTest_ReturnsStartResponse()
    {
        // Arrange
        var response = new StartTestResponse { TestResultId = 1, TestId = 1 };
        _testingServiceMock.Setup(x => x.StartTestAsync(1, 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.StartTest(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartTest_NonExistingTest_ReturnsNotFound()
    {
        // Arrange
        _testingServiceMock.Setup(x => x.StartTestAsync(999, 1))
            .ReturnsAsync((StartTestResponse?)null);

        // Act
        var result = await _controller.StartTest(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswer_ValidAnswer_ReturnsOk()
    {
        // Arrange
        var request = new SubmitAnswerRequest { QuestionId = 1, AnswerId = 1 };
        _testingServiceMock.Setup(x => x.SubmitAnswerAsync(1, 1, It.IsAny<SubmitAnswerRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SubmitAnswer(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FinishTest_ValidTestResult_ReturnsFinishResponse()
    {
        // Arrange
        var response = new FinishTestResponse { Score = 85, Status = "passed" };
        _testingServiceMock.Setup(x => x.FinishTestAsync(1, 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.FinishTest(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResult_ExistingResult_ReturnsResult()
    {
        // Arrange
        var testResult = new TestResultDto { Id = 1, Score = 85 };
        _testingServiceMock.Setup(x => x.GetTestResultAsync(1, 1))
            .ReturnsAsync(testResult);

        // Act
        var result = await _controller.GetResult(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
