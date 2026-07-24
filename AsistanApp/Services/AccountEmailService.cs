using System.Net;
using System.Net.Mail;

namespace ilk_projem.Services;

public sealed class AccountEmailService
{
    private readonly IConfiguration _configuration;

    public AccountEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["App:PublicBaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("App:PublicBaseUrl is not configured.");
        var link =
            $"{baseUrl}/reset-password.html?email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(resetToken)}";

        using var message = new MailMessage(
            _configuration["Email:FromAddress"]
                ?? throw new InvalidOperationException("Email sender is not configured."),
            email,
            "SafeGuardian şifre sıfırlama / Password reset",
            $"Şifrenizi yenilemek için bağlantıyı açın:\n{link}\n\n" +
            $"Open this link to reset your password:\n{link}");

        using var smtp = new SmtpClient(
            _configuration["Email:SmtpServer"],
            _configuration.GetValue("Email:SmtpPort", 587))
        {
            EnableSsl = _configuration.GetValue("Email:UseStartTls", true),
            Credentials = new NetworkCredential(
                _configuration["Email:Username"],
                _configuration["Email:Password"])
        };
        cancellationToken.ThrowIfCancellationRequested();
        await smtp.SendMailAsync(message, cancellationToken);
    }
}
