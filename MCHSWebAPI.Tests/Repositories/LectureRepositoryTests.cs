using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class LectureRepositoryTests
{
    [Fact]
    public void LectureRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(LectureRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(ILectureRepository));
    }
}
