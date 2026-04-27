using MCHSWebAPI.Controllers;

namespace MCHSWebAPI.Tests.Controllers;

public class RolesControllerTests
{
    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _controller = new RolesController(_roleServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithRoles()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "user" },
            new() { Id = 2, Name = "admin" }
        };
        _roleServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(roles);
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList()
    {
        _roleServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Role>());
        var result = await _controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }
}
