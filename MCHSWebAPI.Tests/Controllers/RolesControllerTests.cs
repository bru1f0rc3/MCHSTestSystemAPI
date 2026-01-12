using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;

namespace MCHSWebAPI.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _controller = new RolesController(_roleRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role { Id = 1, Name = "admin" },
            new Role { Id = 2, Name = "user" }
        };
        
        _roleRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(roles);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _roleRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
