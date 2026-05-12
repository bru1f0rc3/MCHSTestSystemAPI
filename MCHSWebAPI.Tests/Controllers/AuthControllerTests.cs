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

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var request = TestDataFactory.CreateLoginRequest();
        _authServiceMock.Setup(s => s.LoginAsync(request)).ReturnsAsync((AuthResponse?)null);

        var result = await _controller.Login(request);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Register_WithNewUser_ReturnsOk()
    {
        var request = TestDataFactory.CreateRegisterRequest();
        var authResponse = TestDataFactory.CreateAuthResponse();
        _authServiceMock.Setup(s => s.RegisterAsync(request)).ReturnsAsync(authResponse);

        var result = await _controller.Register(request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = TestDataFactory.CreateRegisterRequest();
        _authServiceMock.Setup(s => s.RegisterAsync(request)).ReturnsAsync((AuthResponse?)null);

        var result = await _controller.Register(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        var request = TestDataFactory.CreateRegisterRequest();
        _authServiceMock.Setup(s => s.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("Имя занято"));

        var result = await _controller.Register(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegisterGuest_WithDeviceId_ReturnsOk()
    {
        var request = new GuestRegisterRequest { DeviceId = "device-123" };
        var authResponse = TestDataFactory.CreateAuthResponse(role: "guest");
        _authServiceMock.Setup(s => s.RegisterGuestAsync("device-123")).ReturnsAsync(authResponse);

        var result = await _controller.RegisterGuest(request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RegisterGuest_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new GuestRegisterRequest { DeviceId = "device-123" };
        _authServiceMock.Setup(s => s.RegisterGuestAsync("device-123")).ReturnsAsync((AuthResponse?)null);

        var result = await _controller.RegisterGuest(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Success_ReturnsOk()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "old123",
            NewPassword = "newPass1"
        };
        _authServiceMock.Setup(s => s.ChangePasswordAsync(1, request)).ReturnsAsync(true);
        SetUserId(_controller, 1);

        var result = await _controller.ChangePassword(request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ReturnsBadRequest()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "wrong",
            NewPassword = "newPass1"
        };
        _authServiceMock.Setup(s => s.ChangePasswordAsync(1, request)).ReturnsAsync(false);
        SetUserId(_controller, 1);

        var result = await _controller.ChangePassword(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenExists_ReturnsOk()
    {
        var profile = new UserProfileResponse
        {
            UserId = 1,
            Username = "testuser",
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };
        _authServiceMock.Setup(s => s.GetProfileAsync(1)).ReturnsAsync(profile);
        SetUserId(_controller, 1);

        var result = await _controller.GetCurrentUser();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotFound_ReturnsNotFound()
    {
        _authServiceMock.Setup(s => s.GetProfileAsync(1)).ReturnsAsync((UserProfileResponse?)null);
        SetUserId(_controller, 1);

        var result = await _controller.GetCurrentUser();

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteAccount_Success_ReturnsOk()
    {
        _authServiceMock.Setup(s => s.DeleteCurrentUserAsync(1)).ReturnsAsync(true);
        SetUserId(_controller, 1);

        var result = await _controller.DeleteAccount();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteAccount_Failure_ReturnsBadRequest()
    {
        _authServiceMock.Setup(s => s.DeleteCurrentUserAsync(1)).ReturnsAsync(false);
        SetUserId(_controller, 1);

        var result = await _controller.DeleteAccount();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static void SetUserId(ControllerBase controller, int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
