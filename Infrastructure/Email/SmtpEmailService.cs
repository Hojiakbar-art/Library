using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendBookBorrowedAsync(string email, string bookTitle, DateTime dueAt, CancellationToken cancellationToken = default)
    {
        await SendAsync(email, "Book loaned", $"You borrowed '{bookTitle}' and it is due {dueAt:O}.", cancellationToken);
    }

    public async Task SendBookReturnedAsync(string email, string bookTitle, decimal fine, CancellationToken cancellationToken = default)
    {
        var fineText = fine > 0 ? $" The fine is {fine}." : string.Empty;
        await SendAsync(email, "Book returned", $"You returned '{bookTitle}'.{fineText}", cancellationToken);
    }

    public async Task SendOverdueAsync(string email, string bookTitle, DateTime dueAt, decimal finePerDay, CancellationToken cancellationToken = default)
    {
        await SendAsync(email, "Overdue loan", $"Your book '{bookTitle}' was overdue. Due: {dueAt:O}. Fine per day: {finePerDay}.", cancellationToken);
    }

    private async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_settings.UserName) ? null : new NetworkCredential(_settings.UserName, _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(email);
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP send failed for {Email}", email);
        }
    }
}
