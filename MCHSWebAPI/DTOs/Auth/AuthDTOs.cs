using System.ComponentModel.DataAnnotations;
using MCHSWebAPI.Attributes;

namespace MCHSWebAPI.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "��� ������������ �����������")]
    [MinLength(3, ErrorMessage = "��� ������������ ������ ��������� ������� 3 �������")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "������ ����������")]
    [MinLength(6, ErrorMessage = "������ ������ ��������� ������� 6 ��������")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Username]
    public string Username { get; set; } = string.Empty;

    [Password(MinLength = 6)]
    public string Password { get; set; } = string.Empty;

    public string? Email { get; set; }
    
    public string? DeviceId { get; set; }
}

public class GuestRegisterRequest
{
    public string? DeviceId { get; set; }
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
    [Required(ErrorMessage = "������ ������ ����������")]
    public string OldPassword { get; set; } = string.Empty;

    [Password(MinLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
