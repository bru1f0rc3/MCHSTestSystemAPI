using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class TestRepositoryTests
{
    [Fact]
    public void TestRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(TestRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(ITestRepository));
    }
}
