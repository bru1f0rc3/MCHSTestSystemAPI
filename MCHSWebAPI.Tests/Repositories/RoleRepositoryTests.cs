using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class RoleRepositoryTests
{
    [Fact]
    public void RoleRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(RoleRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IRoleRepository));
    }
}
