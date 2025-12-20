using System.Text;
using System.Text.RegularExpressions;
using MCHSProject.DTO.Auth;

namespace MCHSProject.Validators.Auth
{
    public static class AuthValidator
    {
        public static (bool IsValid, string Error) ValidateRegister(RegisterDTO dto)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.AppendLine("Username is required");
            else if (dto.Username.Length < 3)
                errors.AppendLine("Username must be at least 3 characters");
            else if (dto.Username.Length > 50)
                errors.AppendLine("Username must not exceed 50 characters");
            else if (!Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_]+$"))
                errors.AppendLine("Username can only contain letters, numbers and underscore");

            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.AppendLine("Password is required");
            else
            {
                if (dto.Password.Length < 6)
                    errors.AppendLine("Password must be at least 6 characters");
                if (!Regex.IsMatch(dto.Password, @"[A-Z]"))
                    errors.AppendLine("Password must contain at least one uppercase letter");
                if (!Regex.IsMatch(dto.Password, @"[a-z]"))
                    errors.AppendLine("Password must contain at least one lowercase letter");
                if (!Regex.IsMatch(dto.Password, @"[0-9]"))
                    errors.AppendLine("Password must contain at least one digit");
            }

            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                errors.AppendLine("Confirm password is required");
            else if (dto.Password != dto.ConfirmPassword)
                errors.AppendLine("Passwords do not match");

            var errorString = errors.ToString().Trim();
            return (string.IsNullOrEmpty(errorString), errorString);
        }

        public static (bool IsValid, string Error) ValidateLogin(LoginDTO dto)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.AppendLine("Username is required");

            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.AppendLine("Password is required");

            var errorString = errors.ToString().Trim();
            return (string.IsNullOrEmpty(errorString), errorString);
        }

        public static (bool IsValid, string Error) ValidateChangePassword(ChangePasswordDTO dto)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                errors.AppendLine("Current password is required");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                errors.AppendLine("New password is required");
            else
            {
                if (dto.NewPassword.Length < 6)
                    errors.AppendLine("New password must be at least 6 characters");
                if (!Regex.IsMatch(dto.NewPassword, @"[A-Z]"))
                    errors.AppendLine("New password must contain at least one uppercase letter");
                if (!Regex.IsMatch(dto.NewPassword, @"[a-z]"))
                    errors.AppendLine("New password must contain at least one lowercase letter");
                if (!Regex.IsMatch(dto.NewPassword, @"[0-9]"))
                    errors.AppendLine("New password must contain at least one digit");
            }

            if (string.IsNullOrWhiteSpace(dto.ConfirmNewPassword))
                errors.AppendLine("Confirm password is required");
            else if (dto.NewPassword != dto.ConfirmNewPassword)
                errors.AppendLine("Passwords do not match");

            var errorString = errors.ToString().Trim();
            return (string.IsNullOrEmpty(errorString), errorString);
        }

        public static (bool IsValid, string Error) ValidateRefreshToken(RefreshTokenDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return (false, "Refresh token is required");

            return (true, string.Empty);
        }
    }
}
