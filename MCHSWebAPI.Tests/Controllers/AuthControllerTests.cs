using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Controllers;
using MCHSWebAPI.Tests.Helpers;

namespace MCHSWebAPI.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var request = TestDataFactory.CreateLoginRequest();
        var response = TestDataFactory.CreateAuthResponse();
        
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        _authServiceMock.Verify(x => x.LoginAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = TestDataFactory.CreateLoginRequest();
        
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync((AuthResponse?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest();
        var response = TestDataFactory.CreateAuthResponse();
        
        _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_UserExists_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest();
        
        _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync((AuthResponse?)null);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
