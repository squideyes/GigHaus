using System.Net;
using System.Net.Mail;

namespace GigHaus.Infrastructure.Email;

/// <summary>
/// SMTP-based email service. Works with MailDev (dev) and MailGun SMTP (production).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailService(SmtpEmailOptions options)
    {
        _options = options;
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrEmpty(_options.Username)
                ? null
                : new NetworkCredential(_options.Username, _options.Password)
        };

        var from = message.From ?? _options.DefaultFrom;
        var mailMessage = new MailMessage(from, message.To, message.Subject, message.HtmlBody)
        {
            IsBodyHtml = true
        };

        if (!string.IsNullOrEmpty(message.PlainTextBody))
        {
            mailMessage.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.PlainTextBody, null, "text/plain"));
        }

        await client.SendMailAsync(mailMessage, ct);
    }
}

public class SmtpEmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025; // MailDev default
    public bool EnableSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DefaultFrom { get; set; } = "noreply@gighaus.app";
}

