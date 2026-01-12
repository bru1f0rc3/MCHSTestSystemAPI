using MCHSWebAPI.Services;

namespace MCHSWebAPI.Tests.Services;

public class TestingServiceTests
{
    private readonly Mock<ITestRepository> _testRepositoryMock;
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<IAnswerRepository> _answerRepositoryMock;
    private readonly Mock<ITestResultRepository> _testResultRepositoryMock;
    private readonly Mock<IUserAnswerRepository> _userAnswerRepositoryMock;
    private readonly TestingService _testingService;

    public TestingServiceTests()
    {
        _testRepositoryMock = new Mock<ITestRepository>();
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _testResultRepositoryMock = new Mock<ITestResultRepository>();
        _userAnswerRepositoryMock = new Mock<IUserAnswerRepository>();
        _testingService = new TestingService(
            _testRepositoryMock.Object,
            _questionRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _testResultRepositoryMock.Object,
            _userAnswerRepositoryMock.Object);
    }

    [Fact]
    public void TestingService_ShouldInitialize()
    {
        // Assert
        _testingService.Should().NotBeNull();
    }
}
