using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using System.Security.Claims;
using MCHSWebAPI.Services.AuthService.AuthService;
using MCHSWebAPI.Services.VerificationService;

namespace MCHSWebAPI.Controllers.AuthController;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;

    public AuthController(IAuthService authService, IVerificationService verificationService)
    {
        _authService = authService;
        _verificationService = verificationService;
    }
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Неверное имя пользователя или пароль"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Вход выполнен успешно"));
    }
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
                return BadRequest(ApiResponse<AuthResponse>.Fail(
                    "Регистрация не выполнена. Проверьте код подтверждения, логин или email."));

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
        var result = await _authService.RegisterGuestAsync(request.DeviceId ?? string.Empty);
        if (result == null)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Не удалось создать гостевой аккаунт"));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Гостевой аккаунт создан"));
    }

    [HttpPost("guest/status")]
    public async Task<ActionResult<ApiResponse<GuestStatusResponse>>> GuestStatus([FromBody] GuestStatusRequest request)
    {
        var status = await _authService.GetGuestStatusAsync(request.DeviceId);
        return Ok(ApiResponse<GuestStatusResponse>.Ok(status));
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, request);

        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Неверный текущий пароль или код подтверждения"));

        return Ok(ApiResponse<bool>.Ok(true, "Пароль успешно изменен"));
    }
    [Authorize]
    [HttpPost("change-password/request-code")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RequestChangePasswordCode()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var masked = await _authService.SendChangePasswordCodeAsync(userId);
        if (string.IsNullOrWhiteSpace(masked))
            return BadRequest(ApiResponse<MessageResponse>.Fail("Не удалось отправить код (проверьте email профиля)"));

        return Ok(ApiResponse<MessageResponse>.Ok(
            new MessageResponse { Message = "Код отправлен", MaskedEmail = masked },
            $"Код отправлен на {masked}"));
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _authService.GetProfileAsync(userId);
        if (profile == null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("Пользователь не найден"));

        return Ok(ApiResponse<UserProfileResponse>.Ok(profile));
    }
    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _authService.UpdateProfileAsync(userId, request);
        if (profile == null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("Пользователь не найден"));
        return Ok(ApiResponse<UserProfileResponse>.Ok(profile, "Профиль обновлён"));
    }
    [HttpPost("send-code")]
    public async Task<ActionResult<ApiResponse<bool>>> SendCode([FromBody] SendCodeRequest request)
    {
        var validPurposes = new[] { "password_reset", "registration", "password_change", "email_change_current", "email_change_new", "account_delete" };
        if (!validPurposes.Contains(request.Purpose))
            return BadRequest(ApiResponse<bool>.Fail("Недопустимое назначение кода"));

        try
        {
            var result = await _verificationService.SendCodeAsync(request.Email, request.Purpose);
            if (!result)
                return BadRequest(ApiResponse<bool>.Fail("Подождите минуту перед повторной отправкой"));

            return Ok(ApiResponse<bool>.Ok(true, "Код отправлен на " + request.Email));
        }
        catch (Exception)
        {
            return BadRequest(ApiResponse<bool>.Fail("Не удалось отправить код. Проверьте email."));
        }
    }
    [HttpPost("verify-code")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        var result = await _verificationService.VerifyCodeAsync(request.Email, request.Code, request.Purpose);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Неверный или просроченный код"));

        return Ok(ApiResponse<bool>.Ok(true, "Код подтверждён"));
    }
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _verificationService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
        if (!result)
            return BadRequest(ApiResponse<bool>.Fail("Неверный код или пользователь не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Пароль успешно сброшен"));
    }
    [HttpPost("forgot-password/request-code")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> RequestForgotPasswordCode(
        [FromBody] ForgotPasswordRequest request)
    {
        var masked = await _authService.RequestPasswordResetCodeAsync(request.LoginOrEmail);
        if (string.IsNullOrWhiteSpace(masked))
        {
            return BadRequest(ApiResponse<ForgotPasswordResponse>.Fail(
                "Не удалось отправить код. Проверьте логин/email и наличие почты в профиле."));
        }

        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(
            new ForgotPasswordResponse { MaskedEmail = masked },
            $"Код отправлен на {masked}"));
    }
    [HttpPost("forgot-password/confirm")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmForgotPassword(
        [FromBody] ConfirmPasswordResetRequest request)
    {
        var ok = await _authService.ConfirmPasswordResetAsync(
            request.LoginOrEmail, request.Code, request.NewPassword);
        if (!ok)
            return BadRequest(ApiResponse<bool>.Fail("Неверный код или пользователь не найден"));
        return Ok(ApiResponse<bool>.Ok(true, "Пароль успешно восстановлен"));
    }
    [Authorize]
    [HttpPost("change-email/request-current-code")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RequestCurrentEmailCode()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var masked = await _verificationService.RequestCurrentEmailChangeCodeAsync(userId);
        if (string.IsNullOrWhiteSpace(masked))
            return BadRequest(ApiResponse<MessageResponse>.Fail("Текущая почта не найдена или нельзя отправить код"));

        return Ok(ApiResponse<MessageResponse>.Ok(
            new MessageResponse { Message = "Код отправлен", MaskedEmail = masked },
            $"Код отправлен на {masked}"));
    }
    [Authorize]
    [HttpPost("change-email/verify-current")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyCurrentEmailCode(
        [FromBody] ConfirmNewEmailRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ok = await _verificationService.ConfirmCurrentEmailCodeAsync(userId, request.Code);
        if (!ok)
            return BadRequest(ApiResponse<bool>.Fail("Неверный код текущей почты"));
        return Ok(ApiResponse<bool>.Ok(true, "Текущая почта подтверждена"));
    }
    [Authorize]
    [HttpPost("change-email/request-new-code")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RequestNewEmailCode(
        [FromBody] RequestNewEmailCodeRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var masked = await _verificationService.RequestNewEmailCodeAsync(userId, request.NewEmail);
        if (string.IsNullOrWhiteSpace(masked))
            return BadRequest(ApiResponse<MessageResponse>.Fail("Не удалось отправить код на новую почту"));
        return Ok(ApiResponse<MessageResponse>.Ok(
            new MessageResponse { Message = "Код отправлен на новую почту", MaskedEmail = masked },
            $"Код отправлен на {masked}"));
    }
    [Authorize]
    [HttpPost("change-email/confirm-new")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmNewEmail(
        [FromBody] ConfirmNewEmailFinalizeRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _verificationService.ConfirmNewEmailDetailedAsync(userId, request.Code, request.NewEmail);
        if (!result.Success)
            return BadRequest(ApiResponse<bool>.Fail(result.Error ?? "Не удалось подтвердить новую почту"));
        return Ok(ApiResponse<bool>.Ok(true, "Email успешно изменён"));
    }
    [Authorize]
    [HttpPost("delete-account/request-code")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RequestDeleteAccountCode()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var masked = await _authService.SendDeleteAccountCodeAsync(userId);
        if (string.IsNullOrWhiteSpace(masked))
            return BadRequest(ApiResponse<MessageResponse>.Fail("Не удалось отправить код для удаления"));
        return Ok(ApiResponse<MessageResponse>.Ok(
            new MessageResponse { Message = "Код отправлен", MaskedEmail = masked },
            $"Код отправлен на {masked}"));
    }
    [Authorize]
    [HttpPost("delete-account")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ok = await _authService.DeleteCurrentUserAsync(userId, request.Code);
        if (!ok)
            return BadRequest(ApiResponse<bool>.Fail("Не удалось удалить аккаунт (код неверен или есть связанные данные)"));
        return Ok(ApiResponse<bool>.Ok(true, "Аккаунт удалён"));
    }
}
