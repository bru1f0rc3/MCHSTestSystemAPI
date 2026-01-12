namespace MCHSWebAPI.Tests.Helpers;

/// <summary>
/// Фабрика для создания тестовых данных
/// </summary>
public static class TestDataFactory
{
    public static User CreateTestUser(int id = 1, string username = "testuser")
    {
        return new User
        {
            Id = id,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            RoleId = 2,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UserDto CreateTestUserDto(int id = 1, string username = "testuser")
    {
        return new UserDto
        {
            Id = id,
            Username = username,
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Role CreateTestRole(int id = 1, string name = "user")
    {
        return new Role
        {
            Id = id,
            Name = name
        };
    }

    public static LoginRequest CreateLoginRequest(string username = "testuser", string password = "password123")
    {
        return new LoginRequest
        {
            Username = username,
            Password = password
        };
    }

    public static RegisterRequest CreateRegisterRequest(string username = "testuser", string password = "password123")
    {
        return new RegisterRequest
        {
            Username = username,
            Password = password
        };
    }

    public static AuthResponse CreateAuthResponse(int userId = 1, string username = "testuser")
    {
        return new AuthResponse
        {
            UserId = userId,
            Username = username,
            Role = "user",
            Token = "mock_jwt_token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }
}
