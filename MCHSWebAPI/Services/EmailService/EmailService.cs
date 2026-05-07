using System.Net;
using System.Net.Mail;

namespace MCHSWebAPI.Services.EmailService;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code, string purpose)
    {
        var smtpHost = _config["Smtp:Host"] ?? "smtp.mail.ru";
        var smtpPort = _config.GetValue<int>("Smtp:Port", 587);
        var smtpUser = _config["Smtp:Username"] ?? "";
        var smtpPass = _config["Smtp:Password"] ?? "";
        var fromName = _config["Smtp:FromName"] ?? "МЧС Система тестирования";

        var purposeText = purpose switch
        {
            "password_reset" => "Восстановление пароля",
            "email_change" => "Смена email",
            "email_change_current" => "Подтверждение текущего email",
            "email_change_new" => "Подтверждение нового email",
            "registration" => "Подтверждение регистрации",
            "password_change" => "Подтверждение смены пароля",
            "account_delete" => "Удаление аккаунта",
            _ => "Подтверждение"
        };

        var purposeDescription = purpose switch
        {
            "password_reset" => "Вы запросили восстановление пароля в системе тестирования МЧС.",
            "email_change" => "Вы запросили смену email в системе тестирования МЧС.",
            "email_change_current" => "Подтвердите текущий email, чтобы продолжить смену адреса.",
            "email_change_new" => "Подтвердите новый email, чтобы завершить смену адреса.",
            "registration" => "Вы регистрируетесь в системе тестирования МЧС.",
            "password_change" => "Подтвердите смену пароля для вашей учетной записи.",
            "account_delete" => "Подтвердите удаление вашего аккаунта в системе тестирования МЧС.",
            _ => "Вы запросили подтверждение действия."
        };

        var htmlBody = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{purposeText}</title>
</head>
<body style=""margin:0;padding:0;background-color:#eef2ff;font-family:'Segoe UI',Arial,Helvetica,sans-serif;color:#0f172a;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#eef2ff;padding:28px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;width:100%;background-color:#ffffff;border:1px solid #dbe3ff;border-radius:18px;overflow:hidden;box-shadow:0 14px 40px rgba(15,23,42,0.14);"">
          <tr>
            <td style=""padding:0;background:linear-gradient(135deg,#0f172a 0%,#1e3a8a 55%,#2563eb 100%);"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""padding:28px 30px 24px;"">
                    <p style=""margin:0;color:#c7d2fe;font-size:12px;letter-spacing:1.2px;text-transform:uppercase;"">Система тестирования МЧС</p>
                    <h1 style=""margin:8px 0 0;color:#ffffff;font-size:24px;line-height:1.25;font-weight:700;"">{purposeText}</h1>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style=""padding:30px 30px 18px;"">
              <p style=""margin:0 0 16px;font-size:16px;line-height:1.6;color:#334155;"">{purposeDescription}</p>
              <p style=""margin:0 0 14px;font-size:14px;line-height:1.5;color:#64748b;"">Введите код ниже в форме подтверждения:</p>
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:0 0 22px;"">
                <tr>
                  <td align=""center"">
                    <div style=""display:inline-block;padding:16px 24px;background-color:#f8faff;border:1px solid #c7d2fe;border-radius:14px;"">
                      <span style=""font-family:'Consolas','Courier New',monospace;font-size:40px;line-height:1;letter-spacing:10px;color:#1d4ed8;font-weight:700;"">{code}</span>
                    </div>
                  </td>
                </tr>
              </table>
              <div style=""padding:14px 16px;background-color:#fff7ed;border:1px solid #fed7aa;border-radius:12px;"">
                <p style=""margin:0;color:#9a3412;font-size:13px;line-height:1.5;""><strong>Важно:</strong> код действует 3 минуты. Если запрос отправляли не вы, просто проигнорируйте это письмо.</p>
              </div>
            </td>
          </tr>
          <tr>
            <td style=""padding:18px 30px 28px;"">
              <div style=""height:1px;background-color:#e2e8f0;margin-bottom:14px;""></div>
              <p style=""margin:0;color:#94a3b8;font-size:12px;line-height:1.5;"">Это автоматическое письмо, отвечать на него не нужно.</p>
              <p style=""margin:6px 0 0;color:#94a3b8;font-size:12px;line-height:1.5;"">&#169; {DateTime.Now.Year} МЧС России</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        using var message = new MailMessage();
        message.From = new MailAddress(smtpUser, fromName);
        message.To.Add(new MailAddress(toEmail));
        message.Subject = $"{purposeText} — МЧС Тестирование";
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var smtp = new SmtpClient(smtpHost, smtpPort);
        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
        smtp.EnableSsl = true;
        smtp.Timeout = 15000;

        await smtp.SendMailAsync(message);
    }
}
