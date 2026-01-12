using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class UserAnswerRepositoryTests
{
    [Fact]
    public void UserAnswerRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(UserAnswerRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IUserAnswerRepository));
    }
}
