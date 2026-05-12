using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.AuthController;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : AuthorizedControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Неверное имя пользователя или пароль"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Вход выполнен успешно"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await authService.RegisterAsync(request);
            if (result == null)
                return BadRequest(ApiResponse<AuthResponse>.Fail(
                    "Не удалось зарегистрироваться. Возможно, имя пользователя занято."));

            return Ok(ApiResponse<AuthResponse>.Ok(result, "Регистрация прошла успешно"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("guest")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterGuest([FromBody] GuestRegisterRequest request)
    {
        var result = await authService.RegisterGuestAsync(request.DeviceId ?? string.Empty);
        if (result == null)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Не удалось создать гостевой аккаунт"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Гостевой аккаунт создан"));
    }

    [HttpPost("guest/status")]
    public async Task<ActionResult<ApiResponse<GuestStatusResponse>>> GuestStatus([FromBody] GuestStatusRequest request)
    {
        var status = await authService.GetGuestStatusAsync(request.DeviceId);
        return Ok(ApiResponse<GuestStatusResponse>.Ok(status));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var ok = await authService.ChangePasswordAsync(GetUserId(), request);
            if (!ok)
                return BadRequest(ApiResponse<bool>.Fail("Неверный текущий пароль"));

            return Ok(ApiResponse<bool>.Ok(true, "Пароль успешно изменён"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetCurrentUser()
    {
        var profile = await authService.GetProfileAsync(GetUserId());
        if (profile == null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("Пользователь не найден"));

        return Ok(ApiResponse<UserProfileResponse>.Ok(profile));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var profile = await authService.UpdateProfileAsync(GetUserId(), request);
        if (profile == null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("Пользователь не найден"));
        return Ok(ApiResponse<UserProfileResponse>.Ok(profile, "Профиль обновлён"));
    }

    [Authorize]
    [HttpPost("delete-account")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAccount()
    {
        var ok = await authService.DeleteCurrentUserAsync(GetUserId());
        if (!ok)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось удалить аккаунт"));
        return Ok(ApiResponse<bool>.Ok(true, "Аккаунт удалён"));
    }
}
