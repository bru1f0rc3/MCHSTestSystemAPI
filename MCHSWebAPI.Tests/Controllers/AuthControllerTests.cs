using MCHSWebAPI.Controllers;
using MCHSWebAPI.Tests.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

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
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var request = TestDataFactory.CreateLoginRequest();
        var authResponse = TestDataFactory.CreateAuthResponse();
        _authServiceMock.Setup(s => s.LoginAsync(request)).ReturnsAsync(authResponse);
        var result = await _controller.Login(request);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var request = TestDataFactory.CreateLoginRequest();
        _authServiceMock.Setup(s => s.LoginAsync(request)).ReturnsAsync((AuthResponse?)null);
        var result = await _controller.Login(request);
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithNewUser_ReturnsOk()
    {
        var request = TestDataFactory.CreateRegisterRequest();
        var authResponse = TestDataFactory.CreateAuthResponse();
        _authServiceMock.Setup(s => s.RegisterAsync(request)).ReturnsAsync(authResponse);
        var result = await _controller.Register(request);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithExistingUser_ReturnsBadRequest()
    {
        var request = TestDataFactory.CreateRegisterRequest();
        _authServiceMock.Setup(s => s.RegisterAsync(request)).ReturnsAsync((AuthResponse?)null);
        var result = await _controller.Register(request);
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterGuest_ReturnsOk()
    {
        var request = new GuestRegisterRequest { DeviceId = "test-device-id" };
        var authResponse = TestDataFactory.CreateAuthResponse();
        _authServiceMock.Setup(s => s.RegisterGuestAsync("test-device-id")).ReturnsAsync(authResponse);
        var result = await _controller.RegisterGuest(request);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePassword_Success_ReturnsOk()
    {
        var request = new ChangePasswordRequest { OldPassword = "old", NewPassword = "new123" };
        _authServiceMock.Setup(s => s.ChangePasswordAsync(1, request)).ReturnsAsync(true);
        SetupUser(_controller, 1);
        var result = await _controller.ChangePassword(request);
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ReturnsBadRequest()
    {
        var request = new ChangePasswordRequest { OldPassword = "wrong", NewPassword = "new123" };
        _authServiceMock.Setup(s => s.ChangePasswordAsync(1, request)).ReturnsAsync(false);
        SetupUser(_controller, 1);
        var result = await _controller.ChangePassword(request);
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    private static void SetupUser(ControllerBase controller, int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
