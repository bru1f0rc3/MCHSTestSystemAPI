namespace MCHSWebAPI.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public string FullName
    {
        get
        {
            var parts = new[] { LastName, FirstName, Patronymic }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(' ', parts);
        }
    }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
}

public class UpdateUserRequest
{
    public string? Username { get; set; }
    public int? RoleId { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
}

public class UpdateProfileRequest
{
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
}
