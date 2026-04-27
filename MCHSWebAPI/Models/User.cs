namespace MCHSWebAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? DeviceId { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? PendingEmail { get; set; }
    public bool PendingEmailVerified { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RoleName { get; set; }
}
