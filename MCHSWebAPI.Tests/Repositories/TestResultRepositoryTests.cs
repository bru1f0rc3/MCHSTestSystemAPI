using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class TestResultRepositoryTests
{
    [Fact]
    public void TestResultRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(TestResultRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(ITestResultRepository));
    }
}
