using System.Text;
using MCHSProject.DTO.Users;

namespace MCHSProject.Validators.Users
{
    public static class UserValidator
    {
        public static (bool IsValid, string Error) ValidateUserDTO(UserDTO dto)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.AppendLine("Username is required");
            else if (dto.Username.Length < 3)
                errors.AppendLine("Username must be at least 3 characters");
            else if (dto.Username.Length > 50)
                errors.AppendLine("Username must not exceed 50 characters");

            if (string.IsNullOrWhiteSpace(dto.PasswordHash))
                errors.AppendLine("Password is required");

            var errorString = errors.ToString().Trim();
            return (string.IsNullOrEmpty(errorString), errorString);
        }

        public static (bool IsValid, string Error) ValidateDeleteUserDTO(DeleteUserDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
                return (false, "Username is required");

            return (true, string.Empty);
        }
    }
}
