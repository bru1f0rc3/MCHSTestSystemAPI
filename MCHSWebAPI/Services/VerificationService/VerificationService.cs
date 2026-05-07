using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Helpers;
using MCHSWebAPI.Services.EmailService;

namespace MCHSWebAPI.Services.VerificationService;

public class VerificationService : IVerificationService
{
    private readonly IDbConnectionFactory _db;
    private readonly IEmailService _emailService;

    public VerificationService(IDbConnectionFactory db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<bool> SendCodeAsync(string email, string purpose)
    {
        using var connection = _db.CreateConnection();

        var recentExists = await connection.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(SELECT 1 FROM verification_codes
              WHERE email = @Email AND purpose = @Purpose
              AND created_at > NOW() - INTERVAL '60 seconds')",
            new { Email = email, Purpose = purpose });
        if (recentExists) return false;

        await connection.ExecuteAsync(
            @"UPDATE verification_codes SET used = TRUE
              WHERE email = @Email AND purpose = @Purpose AND used = FALSE",
            new { Email = email, Purpose = purpose });

        var code = Random.Shared.Next(100000, 999999).ToString();
        await connection.ExecuteAsync(
            @"INSERT INTO verification_codes (email, code, purpose, expires_at)
              VALUES (@Email, @Code, @Purpose, NOW() + INTERVAL '3 minutes')",
            new { Email = email, Code = code, Purpose = purpose });

        await _emailService.SendVerificationCodeAsync(email, code, purpose);
        return true;
    }

    public async Task<bool> VerifyCodeAsync(string email, string code, string purpose)
    {
        using var connection = _db.CreateConnection();

        var recordId = await connection.ExecuteScalarAsync<int?>(
            @"SELECT id FROM verification_codes
              WHERE email = @Email AND code = @Code AND purpose = @Purpose
              AND used = FALSE AND expires_at > NOW()
              ORDER BY created_at DESC LIMIT 1",
            new { Email = email, Code = code, Purpose = purpose });
        if (!recordId.HasValue) return false;

        await connection.ExecuteAsync(
            "UPDATE verification_codes SET used = TRUE WHERE id = @Id",
            new { Id = recordId.Value });
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
    {
        if (!await VerifyCodeAsync(email, code, "password_reset"))
            return false;

        using var connection = _db.CreateConnection();
        var userId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM users WHERE email = @Email",
            new { Email = email });
        if (!userId.HasValue) return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await connection.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id",
            new { Hash = hash, Id = userId.Value });
        return true;
    }

    public async Task<bool> ConfirmCurrentEmailCodeAsync(int userId, string currentEmailCode)
    {
        using var connection = _db.CreateConnection();
        var currentEmail = await connection.ExecuteScalarAsync<string?>(
            "SELECT email FROM users WHERE id = @Id", new { Id = userId });
        if (string.IsNullOrWhiteSpace(currentEmail)) return false;

        var email = currentEmail.Trim().ToLowerInvariant();
        if (!await VerifyCodeAsync(email, currentEmailCode, "email_change_current"))
            return false;

        await connection.ExecuteAsync(
            "UPDATE users SET pending_email_verified = TRUE WHERE id = @UserId",
            new { UserId = userId });
        return true;
    }

    public async Task<string?> RequestCurrentEmailChangeCodeAsync(int userId)
    {
        using var connection = _db.CreateConnection();
        var currentEmail = await connection.ExecuteScalarAsync<string?>(
            "SELECT email FROM users WHERE id = @Id", new { Id = userId });
        if (string.IsNullOrWhiteSpace(currentEmail)) return null;

        var email = currentEmail.Trim().ToLowerInvariant();
        var sent = await SendCodeAsync(email, "email_change_current");
        return sent ? EmailHelper.MaskEmail(email) : null;
    }

    public async Task<string?> VerifyCurrentEmailAndSendNewCodeAsync(int userId, string currentEmailCode, string newEmail)
    {
        var confirmed = await ConfirmCurrentEmailCodeAsync(userId, currentEmailCode);
        if (!confirmed) return null;
        return await RequestNewEmailCodeAsync(userId, newEmail);
    }

    public async Task<string?> RequestNewEmailCodeAsync(int userId, string newEmail)
    {
        using var connection = _db.CreateConnection();
        var state = await connection.QueryFirstOrDefaultAsync<(string? Email, bool PendingVerified)>(
            "SELECT email AS Email, pending_email_verified AS PendingVerified FROM users WHERE id = @Id",
            new { Id = userId });
        if (!state.PendingVerified) return null;

        var current = state.Email?.Trim().ToLowerInvariant();
        var candidate = newEmail.Trim().ToLowerInvariant();
        if (candidate.Length == 0 || candidate == current) return null;

        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE lower(email)=lower(@Email) AND id <> @UserId)",
            new { Email = candidate, UserId = userId });
        if (exists) return null;

        await connection.ExecuteAsync(
            "UPDATE users SET pending_email = @PendingEmail WHERE id = @UserId",
            new { PendingEmail = candidate, UserId = userId });

        var sent = await SendCodeAsync(candidate, "email_change_new");
        return sent ? EmailHelper.MaskEmail(candidate) : null;
    }

    public async Task<bool> ChangeEmailAsync(int userId, string code, string newEmail)
    {
        var result = await ConfirmNewEmailDetailedAsync(userId, code, newEmail);
        return result.Success;
    }

    public async Task<bool> ConfirmNewEmailAsync(int userId, string newEmailCode)
    {
        var result = await ConfirmNewEmailDetailedAsync(userId, newEmailCode, null);
        return result.Success;
    }

    public async Task<(bool Success, string? Error)> ConfirmNewEmailDetailedAsync(int userId, string newEmailCode, string? newEmail)
    {
        using var connection = _db.CreateConnection();

        var pending = await connection.QueryFirstOrDefaultAsync(
            @"SELECT pending_email AS PendingEmail, pending_email_verified AS PendingEmailVerified
              FROM users WHERE id = @Id",
            new { Id = userId });

        var pendingEmailRaw = pending?.PendingEmail as string;
        string pendingEmail;

        if (!string.IsNullOrWhiteSpace(pendingEmailRaw))
        {
            pendingEmail = pendingEmailRaw.Trim().ToLowerInvariant();
        }
        else
        {
            var candidate = (newEmail ?? "").Trim().ToLowerInvariant();
            if (candidate.Length == 0)
                return (false, "Новая почта не найдена. Запросите код повторно.");
            pendingEmail = candidate;
        }

        if (!await VerifyCodeAsync(pendingEmail, newEmailCode, "email_change_new"))
        {
            var lastCode = await connection.QueryFirstOrDefaultAsync(
                @"SELECT code AS Code, used AS Used, expires_at AS ExpiresAt
                  FROM verification_codes
                  WHERE email = @Email AND purpose = @Purpose
                  ORDER BY created_at DESC LIMIT 1",
                new { Email = pendingEmail, Purpose = "email_change_new" });

            if (lastCode == null)
                return (false, "Код не найден. Нажмите «Отправить».");

            var used = (bool)lastCode.Used;
            var expiresAt = (DateTime)lastCode.ExpiresAt;
            var lastCodeValue = ((string)lastCode.Code).Trim();

            if (used && lastCodeValue == newEmailCode.Trim())
                return (false, "Этот код уже использован. Запросите новый код.");
            if (expiresAt <= DateTime.UtcNow)
                return (false, "Код просрочен. Запросите новый код.");

            return (false, "Неверный код новой почты.");
        }

        var emailExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE lower(email)=lower(@Email) AND id <> @UserId)",
            new { Email = pendingEmail, UserId = userId });
        if (emailExists)
            return (false, "Этот email уже используется другим пользователем.");

        await connection.ExecuteAsync(
            @"UPDATE users
              SET email = @Email, email_verified = TRUE,
                  pending_email = NULL, pending_email_verified = FALSE
              WHERE id = @UserId",
            new { Email = pendingEmail, UserId = userId });

        return (true, null);
    }

}
