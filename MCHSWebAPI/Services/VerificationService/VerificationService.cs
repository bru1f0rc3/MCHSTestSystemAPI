using Dapper;
using MCHSWebAPI.Data;
using MCHSWebAPI.Services.EmailService;

namespace MCHSWebAPI.Services.VerificationService;

public class VerificationService : IVerificationService
{
    private const string PurposePasswordReset = "password_reset";
    private const string PurposeEmailChangeCurrent = "email_change_current";
    private const string PurposeEmailChangeNew = "email_change_new";
    private const string PurposeRegistration = "registration";
    private const string PurposePasswordChange = "password_change";

    private readonly IDbConnectionFactory _db;
    private readonly IEmailService _emailService;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromSeconds(60);

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

        if (recentExists)
            return false;
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

        if (!recordId.HasValue)
            return false;
        await connection.ExecuteAsync(
            "UPDATE verification_codes SET used = TRUE WHERE id = @Id",
            new { Id = recordId.Value });

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
    {
        if (!await VerifyCodeAsync(email, code, PurposePasswordReset))
            return false;

        using var connection = _db.CreateConnection();

        var userId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM users WHERE email = @Email",
            new { Email = email });

        if (!userId.HasValue)
            return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await connection.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id",
            new { Hash = hash, Id = userId.Value });

        return true;
    }

    public async Task<bool> ChangeEmailAsync(int userId, string code, string newEmail)
    {
        var masked = await VerifyCurrentEmailAndSendNewCodeAsync(userId, code, newEmail);
        return !string.IsNullOrWhiteSpace(masked);
    }

    public async Task<string?> RequestCurrentEmailChangeCodeAsync(int userId)
    {
        using var connection = _db.CreateConnection();
        var currentEmail = await connection.ExecuteScalarAsync<string?>(
            "SELECT email FROM users WHERE id = @Id", new { Id = userId });
        if (string.IsNullOrWhiteSpace(currentEmail))
            return null;

        var email = currentEmail.Trim().ToLowerInvariant();
        var sent = await SendCodeAsync(email, PurposeEmailChangeCurrent);
        return sent ? MaskEmail(email) : null;
    }

    public async Task<string?> VerifyCurrentEmailAndSendNewCodeAsync(int userId, string currentEmailCode, string newEmail)
    {
        var confirmed = await ConfirmCurrentEmailCodeAsync(userId, currentEmailCode);
        if (!confirmed) return null;
        return await RequestNewEmailCodeAsync(userId, newEmail);
    }

    public async Task<bool> ConfirmCurrentEmailCodeAsync(int userId, string currentEmailCode)
    {
        using var connection = _db.CreateConnection();
        var currentEmail = await connection.ExecuteScalarAsync<string?>(
            "SELECT email FROM users WHERE id = @Id", new { Id = userId });
        if (string.IsNullOrWhiteSpace(currentEmail))
            return false;
        var current = currentEmail.Trim().ToLowerInvariant();
        if (!await VerifyCodeAsync(current, currentEmailCode, PurposeEmailChangeCurrent))
            return false;

        await connection.ExecuteAsync(
            @"UPDATE users
              SET pending_email_verified = TRUE
              WHERE id = @UserId",
            new { UserId = userId });

        return true;
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
        if (candidate.Length == 0 || candidate == current)
            return null;

        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE lower(email)=lower(@Email) AND id <> @UserId)",
            new { Email = candidate, UserId = userId });
        if (exists)
            return null;

        await connection.ExecuteAsync(
            @"UPDATE users
              SET pending_email = @PendingEmail
              WHERE id = @UserId",
            new { PendingEmail = candidate, UserId = userId });

        var sent = await SendCodeAsync(candidate, PurposeEmailChangeNew);
        return sent ? MaskEmail(candidate) : null;
    }

    public async Task<bool> ConfirmNewEmailAsync(int userId, string newEmailCode)
    {
        var detailed = await ConfirmNewEmailDetailedAsync(userId, newEmailCode, null);
        return detailed.Success;
    }

    public async Task<(bool Success, string? Error)> ConfirmNewEmailDetailedAsync(int userId, string newEmailCode, string? newEmail)
    {
        using var connection = _db.CreateConnection();
        var pending = await connection.QueryFirstOrDefaultAsync(
            @"SELECT pending_email AS PendingEmail,
                     pending_email_verified AS PendingEmailVerified,
                     email AS CurrentEmail
              FROM users
              WHERE id = @Id",
            new { Id = userId });

        var pendingEmailRaw = pending?.PendingEmail as string;
        var pendingVerified = pending?.PendingEmailVerified as bool? ?? false;
        string pendingEmail;

        if (!string.IsNullOrWhiteSpace(pendingEmailRaw))
        {
            pendingEmail = pendingEmailRaw.Trim().ToLowerInvariant();
        }
        else
        {
            var candidate = (newEmail ?? string.Empty).Trim().ToLowerInvariant();
            if (candidate.Length == 0)
                return (false, "Новая почта не найдена. Запросите код повторно.");

            if (!pendingVerified)
            {
                // Фолбэк: если pending_email уже очищен, но пользователь прислал валидный код
                // для newEmail, позволяем завершить смену. Без кода подтверждение не пройдет.
            }

            pendingEmail = candidate;
        }

        if (!await VerifyCodeAsync(pendingEmail, newEmailCode, PurposeEmailChangeNew))
        {
            var lastCode = await connection.QueryFirstOrDefaultAsync(
                @"SELECT code AS Code, used AS Used, expires_at AS ExpiresAt
                  FROM verification_codes
                  WHERE email = @Email AND purpose = @Purpose
                  ORDER BY created_at DESC
                  LIMIT 1",
                new { Email = pendingEmail, Purpose = PurposeEmailChangeNew });

            if (lastCode == null)
                return (false, "Код не найден. Нажмите «Отправить».");

            var used = (bool)lastCode.Used;
            var expiresAt = (DateTime)lastCode.ExpiresAt;
            var lastCodeValue = ((string)lastCode.Code).Trim();
            var normalizedIncoming = newEmailCode.Trim();

            if (used && string.Equals(lastCodeValue, normalizedIncoming, StringComparison.Ordinal))
                return (false, "Этот код уже использован. Запросите новый код.");
            if (expiresAt <= DateTime.UtcNow)
                return (false, "Код просрочен. Запросите новый код.");

            return (false, "Неверный код новой почты.");
        }

        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE lower(email)=lower(@Email) AND id <> @UserId)",
            new { Email = pendingEmail, UserId = userId });
        if (exists)
            return (false, "Этот email уже используется другим пользователем.");

        await connection.ExecuteAsync(
            @"UPDATE users
              SET email = @Email,
                  email_verified = TRUE,
                  pending_email = NULL,
                  pending_email_verified = FALSE
              WHERE id = @UserId",
            new { Email = pendingEmail, UserId = userId });

        return (true, null);
    }

    private static string MaskEmail(string email)
    {
        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 1 || at == trimmed.Length - 1) return trimmed;
        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        if (local.Length <= 4) return $"{local[0]}***@{domain}";
        var prefix = local[..4];
        var suffix = local[^2..];
        return $"{prefix}{new string('*', Math.Max(3, local.Length - 6))}{suffix}@{domain}";
    }
}
