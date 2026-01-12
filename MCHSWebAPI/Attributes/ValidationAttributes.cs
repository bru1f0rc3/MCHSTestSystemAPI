using System.ComponentModel.DataAnnotations;

namespace MCHSWebAPI.Attributes;

/// <summary>
/// Атрибут валидации для обязательных строковых полей
/// </summary>
public class RequiredStringAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 1;
    public int MaxLength { get; set; } = int.MaxValue;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
        {
            return new ValidationResult($"{validationContext.DisplayName} обязательно для заполнения");
        }

        if (str.Length < MinLength)
        {
            return new ValidationResult($"{validationContext.DisplayName} должно содержать минимум {MinLength} символов");
        }

        if (str.Length > MaxLength)
        {
            return new ValidationResult($"{validationContext.DisplayName} должно содержать максимум {MaxLength} символов");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Атрибут валидации для паролей
/// </summary>
public class PasswordAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 6;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrWhiteSpace(password))
        {
            return new ValidationResult("Пароль обязателен для заполнения");
        }

        if (password.Length < MinLength)
        {
            return new ValidationResult($"Пароль должен содержать минимум {MinLength} символов");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Атрибут валидации для имени пользователя
/// </summary>
public class UsernameAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 50;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
        {
            return new ValidationResult("Имя пользователя обязательно для заполнения");
        }

        if (username.Length < MinLength || username.Length > MaxLength)
        {
            return new ValidationResult($"Имя пользователя должно быть от {MinLength} до {MaxLength} символов");
        }

        // Проверка на допустимые символы (буквы, цифры, подчеркивание)
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            return new ValidationResult("Имя пользователя может содержать только буквы, цифры и подчеркивание");
        }

        return ValidationResult.Success;
    }
}
