namespace MCHSWebAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? DeviceId { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties (для удобства)
    public string? RoleName { get; set; }
}
