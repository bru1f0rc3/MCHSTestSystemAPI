using MCHSWebAPI.Controllers;
using MCHSWebAPI.Tests.Helpers;
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
        SetupUser(_controller, 1);
    }

    [Fact]
    public async Task StartTest_WhenTestExists_ReturnsOk()
    {
        var response = new StartTestResponse
        {
            TestResultId = 1,
            TestTitle = "Тест",
            Questions = new List<TestQuestionDto>()
        };
        _testingServiceMock.Setup(s => s.StartTestAsync(1, 1)).ReturnsAsync(response);
        var result = await _controller.StartTest(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task StartTest_WhenTestNotExists_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.StartTestAsync(999, 1)).ReturnsAsync((StartTestResponse?)null);
        var result = await _controller.StartTest(999);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task FinishTest_Success_ReturnsOk()
    {
        var response = new FinishTestResponse { Score = 80, TotalQuestions = 10, CorrectAnswers = 8, Status = "passed" };
        _testingServiceMock.Setup(s => s.FinishTestAsync(1, 1)).ReturnsAsync(response);
        var result = await _controller.FinishTest(1);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task FinishTest_NotFound_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.FinishTestAsync(999, 1)).ReturnsAsync((FinishTestResponse?)null);
        var result = await _controller.FinishTest(999);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswer_Success_ReturnsOk()
    {
        var request = new SubmitAnswerRequest { QuestionId = 1, AnswerId = 1 };
        _testingServiceMock.Setup(s => s.SubmitAnswerAsync(1, 1, request)).ReturnsAsync(true);
        var result = await _controller.SubmitAnswer(1, request);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyResults_ReturnsOk()
    {
        var results = TestDataFactory.CreatePagedResponse(new List<TestResultDto>());
        _testingServiceMock.Setup(s => s.GetUserResultsAsync(1, 1, 20)).ReturnsAsync(results);
        var result = await _controller.GetMyResults();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    private static void SetupUser(ControllerBase controller, int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
