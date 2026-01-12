using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
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
    public async Task GetAll_ReturnsOkResult()
    {
        // Arrange
        var users = new List<UserDto>
        {
            TestDataFactory.CreateTestUserDto(1, "user1"),
            TestDataFactory.CreateTestUserDto(2, "user2")
        };
        
        var pagedResponse = new PagedResponse<UserDto>
        {
            Items = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        
        _userServiceMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAll(1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUserDto();
        _userServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingUser_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var actionResult = result as ActionResult<ApiResponse<bool>>;
        actionResult.Should().NotBeNull();
        var okResult = actionResult!.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }
}
