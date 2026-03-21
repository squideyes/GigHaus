namespace GigHaus.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? From = null,
    string? PlainTextBody = null
);

