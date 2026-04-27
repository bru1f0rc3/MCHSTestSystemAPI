namespace MCHSWebAPI.Services.VerificationService;

public interface IVerificationService
{
    Task<bool> SendCodeAsync(string email, string purpose);
    Task<bool> VerifyCodeAsync(string email, string code, string purpose);
    Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
    Task<bool> ChangeEmailAsync(int userId, string code, string newEmail);
    Task<string?> RequestCurrentEmailChangeCodeAsync(int userId);
    Task<string?> VerifyCurrentEmailAndSendNewCodeAsync(int userId, string currentEmailCode, string newEmail);
    Task<bool> ConfirmCurrentEmailCodeAsync(int userId, string currentEmailCode);
    Task<string?> RequestNewEmailCodeAsync(int userId, string newEmail);
    Task<bool> ConfirmNewEmailAsync(int userId, string newEmailCode);
    Task<(bool Success, string? Error)> ConfirmNewEmailDetailedAsync(int userId, string newEmailCode, string? newEmail);
}
