using MCHSWebAPI.Tests.Helpers;

namespace MCHSWebAPI.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var users = new List<UserDto> { TestDataFactory.CreateUserDto() };
        var paged = TestDataFactory.CreatePagedResponse(users);
        _userServiceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var user = TestDataFactory.CreateUserDto();
        _userServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _controller.GetById(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        _userServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((UserDto?)null);

        var result = await _controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var request = new CreateUserRequest { Username = "newuser", Password = "pass1234", RoleId = 1 };
        var user = TestDataFactory.CreateUserDto();
        _userServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(user);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenUsernameExists_ReturnsBadRequest()
    {
        var request = new CreateUserRequest { Username = "existing", Password = "pass1234", RoleId = 1 };
        _userServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync((UserDto?)null);

        var result = await _controller.Create(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk()
    {
        var request = new UpdateUserRequest { Username = "updated" };
        _userServiceMock.Setup(s => s.UpdateAsync(1, request)).ReturnsAsync(true);

        var result = await _controller.Update(1, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        var request = new UpdateUserRequest { Username = "updated" };
        _userServiceMock.Setup(s => s.UpdateAsync(999, request)).ReturnsAsync(false);

        var result = await _controller.Update(999, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsOk()
    {
        _userServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        _userServiceMock.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
