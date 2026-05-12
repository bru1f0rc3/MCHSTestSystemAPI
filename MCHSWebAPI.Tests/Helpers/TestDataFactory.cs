namespace MCHSWebAPI.Tests.Helpers;

public static class TestDataFactory
{
    public static AuthResponse CreateAuthResponse(int userId = 1, string username = "testuser", string role = "user")
    {
        return new AuthResponse
        {
            Token = "test-jwt-token",
            UserId = userId,
            Username = username,
            Role = role,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    public static LoginRequest CreateLoginRequest(string username = "testuser", string password = "password123")
    {
        return new LoginRequest { Username = username, Password = password };
    }

    public static RegisterRequest CreateRegisterRequest(string username = "newuser", string password = "password123")
    {
        return new RegisterRequest
        {
            Username = username,
            Password = password
        };
    }

    public static LectureDto CreateLectureDto(int id = 1, string title = "Тестовая лекция")
    {
        return new LectureDto
        {
            Id = id,
            Title = title,
            TextContent = "Содержание лекции",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static CreateLectureRequest CreateLectureRequest(string title = "Новая лекция")
    {
        return new CreateLectureRequest { Title = title, TextContent = "Содержание" };
    }

    public static TestDto CreateTestDto(int id = 1, string title = "Тестовый тест")
    {
        return new TestDto
        {
            Id = id,
            Title = title,
            Description = "Описание теста",
            QuestionsCount = 10,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UserDto CreateUserDto(int id = 1, string username = "testuser")
    {
        return new UserDto
        {
            Id = id,
            Username = username,
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PagedResponse<T> CreatePagedResponse<T>(List<T> items, int page = 1, int pageSize = 20)
    {
        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = items.Count,
            Page = page,
            PageSize = pageSize
        };
    }
}
