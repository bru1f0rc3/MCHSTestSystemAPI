using MCHSWebAPI.Tests.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace MCHSWebAPI.Tests.Controllers;

public class TestsControllerTests
{
    private readonly Mock<ITestService> _testServiceMock;
    private readonly TestsController _controller;

    public TestsControllerTests()
    {
        _testServiceMock = new Mock<ITestService>();
        _controller = new TestsController(_testServiceMock.Object);
        SetAdmin(_controller, 1);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var tests = new List<TestDto> { TestDataFactory.CreateTestDto() };
        var paged = TestDataFactory.CreatePagedResponse(tests);
        _testServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var test = TestDataFactory.CreateTestDto();
        _testServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(test);

        var result = await _controller.GetById(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((TestDto?)null);

        var result = await _controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByIdWithQuestions_WhenExists_ReturnsOk()
    {
        var detail = new TestDetailDto { Id = 1, Title = "Тест" };
        _testServiceMock.Setup(s => s.GetByIdWithQuestionsAsync(1, true)).ReturnsAsync(detail);

        var result = await _controller.GetByIdWithQuestions(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByIdWithQuestions_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.GetByIdWithQuestionsAsync(999, true)).ReturnsAsync((TestDetailDto?)null);

        var result = await _controller.GetByIdWithQuestions(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAvailable_ReturnsOk()
    {
        var paged = TestDataFactory.CreatePagedResponse(new List<TestDto> { TestDataFactory.CreateTestDto() });
        _testServiceMock.Setup(s => s.GetAvailableForUserAsync(1, 1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetAvailable();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByLectureId_ReturnsOk()
    {
        var tests = new List<TestDto> { TestDataFactory.CreateTestDto() };
        _testServiceMock.Setup(s => s.GetByLectureIdAsync(1)).ReturnsAsync(tests);

        var result = await _controller.GetByLectureId(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = new CreateTestRequest { Title = "Новый тест", LectureId = 1 };
        var test = TestDataFactory.CreateTestDto();
        _testServiceMock.Setup(s => s.CreateAsync(request, 1)).ReturnsAsync(test);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = new CreateTestRequest { Title = "Тест", LectureId = 1 };
        _testServiceMock.Setup(s => s.CreateAsync(request, 1)).ReturnsAsync((TestDto?)null);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk()
    {
        var request = new UpdateTestRequest { Title = "Новое название" };
        _testServiceMock.Setup(s => s.UpdateAsync(1, request)).ReturnsAsync(true);

        var result = await _controller.Update(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        var request = new UpdateTestRequest { Title = "Новое название" };
        _testServiceMock.Setup(s => s.UpdateAsync(999, request)).ReturnsAsync(false);

        var result = await _controller.Update(999, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _testServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddQuestion_WhenSuccess_ReturnsOk()
    {
        var request = new CreateQuestionRequest { QuestionText = "Вопрос?", Position = 1 };
        var question = new QuestionDto { Id = 1, QuestionText = "Вопрос?" };
        _testServiceMock.Setup(s => s.AddQuestionAsync(1, request)).ReturnsAsync(question);

        var result = await _controller.AddQuestion(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddQuestion_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = new CreateQuestionRequest { QuestionText = "Вопрос?", Position = 1 };
        _testServiceMock.Setup(s => s.AddQuestionAsync(1, request)).ReturnsAsync((QuestionDto?)null);

        var result = await _controller.AddQuestion(1, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteQuestion_WhenExists_ReturnsOk()
    {
        _testServiceMock.Setup(s => s.DeleteQuestionAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteQuestion(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteQuestion_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.DeleteQuestionAsync(999)).ReturnsAsync(false);

        var result = await _controller.DeleteQuestion(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateQuestion_WhenExists_ReturnsOk()
    {
        var request = new UpdateQuestionRequest { QuestionText = "Обновлено" };
        _testServiceMock.Setup(s => s.UpdateQuestionAsync(1, request)).ReturnsAsync(true);

        var result = await _controller.UpdateQuestion(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateQuestion_WhenNotExists_ReturnsNotFound()
    {
        var request = new UpdateQuestionRequest { QuestionText = "Обновлено" };
        _testServiceMock.Setup(s => s.UpdateQuestionAsync(999, request)).ReturnsAsync(false);

        var result = await _controller.UpdateQuestion(999, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddAnswer_WhenSuccess_ReturnsOk()
    {
        var request = new CreateAnswerRequest { AnswerText = "Ответ", IsCorrect = true, Position = 1 };
        var answer = new AnswerDto { Id = 1, AnswerText = "Ответ", IsCorrect = true };
        _testServiceMock.Setup(s => s.AddAnswerAsync(1, request)).ReturnsAsync(answer);

        var result = await _controller.AddAnswer(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddAnswer_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = new CreateAnswerRequest { AnswerText = "Ответ", Position = 1 };
        _testServiceMock.Setup(s => s.AddAnswerAsync(1, request)).ReturnsAsync((AnswerDto?)null);

        var result = await _controller.AddAnswer(1, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateAnswer_WhenExists_ReturnsOk()
    {
        var request = new UpdateAnswerRequest { AnswerText = "Обновлено" };
        _testServiceMock.Setup(s => s.UpdateAnswerAsync(1, request)).ReturnsAsync(true);

        var result = await _controller.UpdateAnswer(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateAnswer_WhenNotExists_ReturnsNotFound()
    {
        var request = new UpdateAnswerRequest { AnswerText = "Обновлено" };
        _testServiceMock.Setup(s => s.UpdateAnswerAsync(999, request)).ReturnsAsync(false);

        var result = await _controller.UpdateAnswer(999, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteAnswer_WhenExists_ReturnsOk()
    {
        _testServiceMock.Setup(s => s.DeleteAnswerAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteAnswer(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteAnswer_WhenNotExists_ReturnsNotFound()
    {
        _testServiceMock.Setup(s => s.DeleteAnswerAsync(999)).ReturnsAsync(false);

        var result = await _controller.DeleteAnswer(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ImportFromPdf_WhenSuccess_ReturnsOk()
    {
        var request = BuildImportRequest();
        var dto = TestDataFactory.CreateTestDto();
        _testServiceMock
            .Setup(s => s.ImportFromPdfAsync(1, "Title", null, null, It.IsAny<Stream>(), 1))
            .ReturnsAsync(dto);

        var result = await _controller.ImportFromPdf(request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ImportFromPdf_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = BuildImportRequest();
        _testServiceMock
            .Setup(s => s.ImportFromPdfAsync(1, "Title", null, null, It.IsAny<Stream>(), 1))
            .ReturnsAsync((TestDto?)null);

        var result = await _controller.ImportFromPdf(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ImportFromPdf_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        var request = BuildImportRequest();
        _testServiceMock
            .Setup(s => s.ImportFromPdfAsync(1, "Title", null, null, It.IsAny<Stream>(), 1))
            .ThrowsAsync(new InvalidOperationException("Не удалось распарсить PDF"));

        var result = await _controller.ImportFromPdf(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static ImportTestFromPdfDto BuildImportRequest()
    {
        var bytes = Encoding.UTF8.GetBytes("fake-pdf");
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "PdfFile", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        return new ImportTestFromPdfDto
        {
            LectureId = 1,
            Title = "Title",
            PdfFile = file
        };
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
