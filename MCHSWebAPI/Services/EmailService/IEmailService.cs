namespace MCHSWebAPI.Services.EmailService;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string toEmail, string code, string purpose);
}
