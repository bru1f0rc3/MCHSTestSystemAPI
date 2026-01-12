using MCHSWebAPI.Services;
using MCHSWebAPI.Tests.Helpers;

namespace MCHSWebAPI.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _userService = new UserService(_userRepositoryMock.Object, _roleRepositoryMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();
        var role = TestDataFactory.CreateTestRole(2, "user");
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(role);

        // Act
        var result = await _userService.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_ReturnsTrue()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
    }
}
