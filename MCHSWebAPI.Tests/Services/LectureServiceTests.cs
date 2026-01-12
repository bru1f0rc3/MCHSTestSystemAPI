using MCHSWebAPI.Services;

namespace MCHSWebAPI.Tests.Services;

public class LectureServiceTests
{
    private readonly Mock<ILectureRepository> _lectureRepositoryMock;
    private readonly LectureService _lectureService;

    public LectureServiceTests()
    {
        _lectureRepositoryMock = new Mock<ILectureRepository>();
        _lectureService = new LectureService(_lectureRepositoryMock.Object);
    }

    [Fact]
    public void LectureService_ShouldInitialize()
    {
        // Assert
        _lectureService.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Arrange
        _lectureRepositoryMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _lectureService.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        _lectureRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
    }
}
