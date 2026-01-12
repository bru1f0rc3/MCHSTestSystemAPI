using MCHSWebAPI.Services;
using MCHSWebAPI.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace MCHSWebAPI.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup configuration indexer properly
        _configurationMock.Setup(x => x["Jwt:Key"]).Returns("test-secret-key-with-minimum-length-requirement-for-hmac-sha256");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        
        // Setup GetValue for ExpirationHours
        var expirationSection = new Mock<IConfigurationSection>();
        expirationSection.Setup(x => x.Value).Returns("24");
        _configurationMock.Setup(x => x.GetSection("Jwt:ExpirationHours")).Returns(expirationSection.Object);
        
        _authService = new AuthService(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _configurationMock.Object);
    }

    private IConfigurationSection CreateConfigSection(string value)
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(x => x.Value).Returns(value);
        return section.Object;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = TestDataFactory.CreateTestUser();
        var role = TestDataFactory.CreateTestRole(2, "user");
        var request = TestDataFactory.CreateLoginRequest();

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(role);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ReturnsNull()
    {
        // Arrange
        var request = TestDataFactory.CreateLoginRequest("nonexistent", "password");
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsAuthResponse()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest();
        var guestRole = TestDataFactory.CreateTestRole(3, "guest");
        
        _userRepositoryMock.Setup(x => x.ExistsAsync("testuser"))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(1);
        _roleRepositoryMock.Setup(x => x.GetByNameAsync("guest"))
            .ReturnsAsync(guestRole);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task RegisterAsync_ExistingUser_ReturnsNull()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest();
        var guestRole = TestDataFactory.CreateTestRole(3, "guest");
        
        _userRepositoryMock.Setup(x => x.ExistsAsync("testuser"))
            .ReturnsAsync(true);
        _roleRepositoryMock.Setup(x => x.GetByNameAsync("guest"))
            .ReturnsAsync(guestRole);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().BeNull();
    }
}
