using MCHSWebAPI.Repositories;
using MCHSWebAPI.Tests.Helpers;

namespace MCHSWebAPI.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public void UserRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(UserRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IUserRepository));
    }
}
