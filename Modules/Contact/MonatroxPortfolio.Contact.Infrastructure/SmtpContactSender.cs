using System.Net;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MonatroxPortfolio.Contact.Application.Abstractions;
using MonatroxPortfolio.Contact.Domain;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Contact.Infrastructure;

public sealed class SmtpOptions
{
    public string Mode { get; set; } = "Disabled";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 25;
    public string Security { get; set; } = "StartTls";
    public string From { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class SmtpContactSender(IOptions<SmtpOptions> options, IHostEnvironment environment, ILogger<SmtpContactSender> logger) : IContactSender
{
    private SmtpOptions Settings => options.Value;
    public ContactDeliveryInfoDto Info
    {
        get
        {
            var s = Settings;
            var capture = s.Mode == "Capture";
            var local = s.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                (IPAddress.TryParse(s.Host, out var ip) && IPAddress.IsLoopback(ip));
            var valid = !string.IsNullOrWhiteSpace(s.Host) && s.Port is > 0 and <= 65535 && MailboxAddress.TryParse(s.From, out _) &&
                (capture ? !environment.IsProduction() && local && s.Security == "None" : s.Mode == "Send" && s.Security is "StartTls" or "SslOnConnect");
            return new(valid, capture);
        }
    }

    public async Task<Result<ContactReceiptDto>> SendAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        var info = Info;
        if (!info.Available) return Unavailable();
        var s = Settings;
        var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(s.From));
        mail.To.Add(MailboxAddress.Parse(ContactMessage.Recipient));
        mail.ReplyTo.Add(new MailboxAddress(message.Name, message.Email));
        mail.Subject = "Portfolio contact";
        mail.Body = new TextPart("plain") { Text = $"Name: {message.Name}\nReply address: {message.Email}\n\n{message.Body}" };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var client = new SmtpClient { Timeout = 10000 };
            var security = s.Security switch { "None" => SecureSocketOptions.None, "SslOnConnect" => SecureSocketOptions.SslOnConnect, _ => SecureSocketOptions.StartTls };
            await client.ConnectAsync(s.Host, s.Port, security, deadline.Token);
            if (!string.IsNullOrWhiteSpace(s.Username)) await client.AuthenticateAsync(s.Username, s.Password ?? "", deadline.Token);
            await client.SendAsync(mail, deadline.Token);
            // Acceptance is final; a failed QUIT must not turn an accepted message into a retry.
            return Result<ContactReceiptDto>.Success(new(info.CapturedLocally));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Contact SMTP operation timed out.");
            return Unavailable();
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException or SslHandshakeException or IOException or SocketException or NotSupportedException)
        {
            // SMTP exception text can contain visitor data; log type only, never bodies or credentials.
            logger.LogWarning("Contact SMTP operation failed ({FailureType}).", ex.GetType().Name);
            return Unavailable();
        }
    }

    private static Result<ContactReceiptDto> Unavailable() => Result<ContactReceiptDto>.Failure("contact.unavailable", "Email service is unavailable. Please retry later.");
}
