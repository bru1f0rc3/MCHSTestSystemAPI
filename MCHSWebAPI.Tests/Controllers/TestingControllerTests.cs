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
        SetUserId(_controller, 1);
    }

    [Fact]
    public async Task StartTest_WhenTestExists_ReturnsOk()
    {
        var response = new StartTestResponse
        {
            TestResultId = 1,
            TestId = 1,
            TestTitle = "Тест",
            StartedAt = DateTime.UtcNow,
            Questions = new List<TestQuestionDto>()
        };
        _testingServiceMock.Setup(s => s.StartTestAsync(1, 1)).ReturnsAsync(response);

        var result = await _controller.StartTest(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartTest_WhenTestNotExists_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.StartTestAsync(999, 1)).ReturnsAsync((StartTestResponse?)null);

        var result = await _controller.StartTest(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswer_Success_ReturnsOk()
    {
        var request = new SubmitAnswerRequest { QuestionId = 1, AnswerId = 2 };
        _testingServiceMock.Setup(s => s.SubmitAnswerAsync(1, 1, request)).ReturnsAsync(true);

        var result = await _controller.SubmitAnswer(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswer_Failure_ReturnsBadRequest()
    {
        var request = new SubmitAnswerRequest { QuestionId = 1, AnswerId = 2 };
        _testingServiceMock.Setup(s => s.SubmitAnswerAsync(1, 1, request)).ReturnsAsync(false);

        var result = await _controller.SubmitAnswer(1, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswers_Success_ReturnsOk()
    {
        var request = new SubmitAnswersRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = 1, AnswerId = 2 }
            }
        };
        _testingServiceMock.Setup(s => s.SubmitAnswersAsync(1, 1, request)).ReturnsAsync(true);

        var result = await _controller.SubmitAnswers(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FinishTest_Success_ReturnsOk()
    {
        var response = new FinishTestResponse
        {
            TestResultId = 1,
            Score = 80,
            TotalQuestions = 10,
            CorrectAnswers = 8,
            Status = "passed"
        };
        _testingServiceMock.Setup(s => s.FinishTestAsync(1, 1)).ReturnsAsync(response);

        var result = await _controller.FinishTest(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FinishTest_NotFound_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.FinishTestAsync(999, 1)).ReturnsAsync((FinishTestResponse?)null);

        var result = await _controller.FinishTest(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetResult_WhenExists_ReturnsOk()
    {
        var dto = new TestResultDto { Id = 1, TestId = 1, TestTitle = "Тест", Status = "passed" };
        _testingServiceMock.Setup(s => s.GetTestResultAsync(1, 1)).ReturnsAsync(dto);

        var result = await _controller.GetResult(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResult_WhenNotExists_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.GetTestResultAsync(999, 1)).ReturnsAsync((TestResultDto?)null);

        var result = await _controller.GetResult(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMyResults_ReturnsOk()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<TestResultDto>());
        _testingServiceMock.Setup(s => s.GetUserResultsAsync(1, 1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetMyResults();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SubmitAnswers_Failure_ReturnsBadRequest()
    {
        var request = new SubmitAnswersRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = 1, AnswerId = 2 }
            }
        };
        _testingServiceMock.Setup(s => s.SubmitAnswersAsync(1, 1, request)).ReturnsAsync(false);

        var result = await _controller.SubmitAnswers(1, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReportCheatAttempt_Success_ReturnsOk()
    {
        var request = new ReportCheatAttemptRequest { EventType = "app_background" };
        _testingServiceMock
            .Setup(s => s.RegisterCheatAttemptAsync(1, 1, request))
            .ReturnsAsync(true);

        var result = await _controller.ReportCheatAttempt(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReportCheatAttempt_Failure_ReturnsBadRequest()
    {
        var request = new ReportCheatAttemptRequest { EventType = "app_background" };
        _testingServiceMock
            .Setup(s => s.RegisterCheatAttemptAsync(1, 1, request))
            .ReturnsAsync(false);

        var result = await _controller.ReportCheatAttempt(1, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReportCheatAttempt_WithNullBody_PassesDefaultRequest()
    {
        _testingServiceMock
            .Setup(s => s.RegisterCheatAttemptAsync(1, 1, It.IsAny<ReportCheatAttemptRequest>()))
            .ReturnsAsync(true);

        var result = await _controller.ReportCheatAttempt(1, null);

        result.Result.Should().BeOfType<OkObjectResult>();
        _testingServiceMock.Verify(
            s => s.RegisterCheatAttemptAsync(1, 1, It.IsAny<ReportCheatAttemptRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetResultDetail_WhenExists_ReturnsOk()
    {
        var dto = new TestResultDetailDto
        {
            Id = 1,
            TestId = 1,
            TestTitle = "Тест",
            Status = "passed"
        };
        _testingServiceMock.Setup(s => s.GetTestResultDetailAsync(1, 1)).ReturnsAsync(dto);

        var result = await _controller.GetResultDetail(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResultDetail_WhenNotExists_ReturnsNotFound()
    {
        _testingServiceMock.Setup(s => s.GetTestResultDetailAsync(999, 1))
            .ReturnsAsync((TestResultDetailDto?)null);

        var result = await _controller.GetResultDetail(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllResults_ReturnsOk()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<TestResultDto>());
        _testingServiceMock
            .Setup(s => s.GetAllResultsAsync(1, 20, null, null, null))
            .ReturnsAsync(paged);

        var result = await _controller.GetAllResults();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private static void SetUserId(ControllerBase controller, int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
