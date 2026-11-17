using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace OpenQMS.Services;

public class ConfiguredEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public ConfiguredEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.From) || string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Email delivery is not configured. Configure the Email settings before sending authentication links.");
        }

        using var message = new MailMessage(_options.From, email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(message);
    }
}
