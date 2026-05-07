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

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOk()
    {
        _roleServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Role>());

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
