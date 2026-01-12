using MCHSWebAPI.Services;

namespace MCHSWebAPI.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _reportRepositoryMock;
    private readonly Mock<ITestResultRepository> _testResultRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITestRepository> _testRepositoryMock;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _reportRepositoryMock = new Mock<IReportRepository>();
        _testResultRepositoryMock = new Mock<ITestResultRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _testRepositoryMock = new Mock<ITestRepository>();
        _reportService = new ReportService(_reportRepositoryMock.Object);
    }

    [Fact]
    public void ReportService_ShouldInitialize()
    {
        // Assert
        _reportService.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        _reportRepositoryMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _reportService.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        _reportRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
    }
}
