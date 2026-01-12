using MCHSWebAPI.Services;

namespace MCHSWebAPI.Tests.Services;

public class TestServiceTests
{
    private readonly Mock<ITestRepository> _testRepositoryMock;
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<IAnswerRepository> _answerRepositoryMock;
    private readonly Mock<IPdfParserService> _pdfParserServiceMock;
    private readonly TestService _testService;

    public TestServiceTests()
    {
        _testRepositoryMock = new Mock<ITestRepository>();
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _pdfParserServiceMock = new Mock<IPdfParserService>();
        _testService = new TestService(
            _testRepositoryMock.Object,
            _questionRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _pdfParserServiceMock.Object);
    }

    [Fact]
    public void TestService_ShouldInitialize()
    {
        // Assert
        _testService.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CallsRepository()
    {
        // Arrange
        var test = new Test { Id = 1, Title = "Test 1" };
        _testRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(test);

        // Act
        var result = await _testService.GetByIdAsync(1);

        // Assert
        _testRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        _testRepositoryMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _testService.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        _testRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
    }
}
