using System.ComponentModel.DataAnnotations;

namespace MCHSWebAPI.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [MinLength(3, ErrorMessage = "Минимум 3 символа")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Минимум 6 символов")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [MinLength(3, ErrorMessage = "Минимум 3 символа")]
    [MaxLength(50, ErrorMessage = "Максимум 50 символов")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Минимум 6 символов")]
    public string Password { get; set; } = string.Empty;

    public string? DeviceId { get; set; }

    [MaxLength(100)] public string? LastName { get; set; }
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? Patronymic { get; set; }
}

public class GuestRegisterRequest
{
    public string? DeviceId { get; set; }
}

public class GuestStatusRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

public class GuestStatusResponse
{
    public bool HasExistingAccount { get; set; }
    public bool IsGuestAccount { get; set; }
    public string? Username { get; set; }
}

public class AuthResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Старый пароль обязателен")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [MinLength(6, ErrorMessage = "Минимум 6 символов")]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
    public DateTime CreatedAt { get; set; }
}
