using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.Interfaces.Services;
using MCHSWebAPI.DTOs.Auth;
using MCHSWebAPI.DTOs.Common;
using System.Security.Claims;

namespace MCHSWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Неверное имя пользователя или пароль"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Вход выполнен успешно"));
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Пользователь с таким именем уже существует"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Регистрация прошла успешно"));
    }

    /// <summary>
    /// Быстрая регистрация гостевого пользователя
    /// </summary>
    [HttpPost("guest")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterGuest([FromBody] GuestRegisterRequest request)
    {
        var result = await _authService.RegisterGuestAsync(request.DeviceId ?? string.Empty);
        if (result == null)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Не удалось создать гостевой аккаунт"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Гостевой аккаунт создан"));
    }

    /// <summary>
    /// Смена пароля
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, request);
        
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Неверный текущий пароль"));

        return Ok(ApiResponse<bool>.Ok(true, "Пароль успешно изменен"));
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(ApiResponse<object>.Ok(new
        {
            UserId = userId,
            Username = username,
            Role = role
        }));
    }
}
