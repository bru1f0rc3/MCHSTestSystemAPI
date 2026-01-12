using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class AnswerRepositoryTests
{
    [Fact]
    public void AnswerRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(AnswerRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IAnswerRepository));
    }
}
