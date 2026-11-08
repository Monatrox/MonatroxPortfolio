using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using MonatroxPortfolio.Contact.Domain;
using MonatroxPortfolio.Contact.Infrastructure;

namespace MonatroxPortfolio.Contact.Tests;

public sealed class SmtpTests
{
    private sealed class Environment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "ContactTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private static SmtpContactSender Sender(SmtpOptions settings, string environment = "Test") =>
        new(Options.Create(settings), new Environment { EnvironmentName = environment }, NullLogger<SmtpContactSender>.Instance);

    [Theory]
    [InlineData("Capture", "None", "Production", false)]
    [InlineData("Capture", "None", "Test", true)]
    [InlineData("Send", "None", "Production", false)]
    [InlineData("Send", "StartTls", "Production", true)]
    [InlineData("Send", "SslOnConnect", "Production", true)]
    [InlineData("Disabled", "StartTls", "Test", false)]
    public void Configuration_does_not_silently_allow_plaintext_live_delivery(string mode, string security, string environment, bool available)
    {
        var sender = Sender(new() { Mode = mode, Security = security, Host = "127.0.0.1", From = "portfolio@example.com" }, environment);
        Assert.Equal(available, sender.Info.Available);
        Assert.False(Sender(new() { Mode = "Capture", Security = "None", Host = "external.example", From = "portfolio@example.com" }).Info.Available);
    }

    [Fact]
    public async Task Smtp_envelope_is_fixed_reply_to_is_visitor_and_body_is_plain_text()
    {
        using var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var received = ReceiveAsync(server, deadline.Token);
        var sender = Sender(new()
        {
            Host = "127.0.0.1", Port = ((IPEndPoint)server.LocalEndpoint).Port,
            Mode = "Capture", Security = "None", From = "Portfolio <portfolio@monatrox.invalid>"
        });
        var result = await sender.SendAsync(new("Visitor", "visitor@example.com", "Hello <script>literal</script>\n.line"), deadline.Token);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CapturedLocally);
        var (recipient, mail) = await received;
        Assert.Contains("<zac@monatrox.com>", recipient);
        Assert.Equal("zac@monatrox.com", Assert.Single(mail.To.Mailboxes).Address);
        Assert.Equal("portfolio@monatrox.invalid", Assert.Single(mail.From.Mailboxes).Address);
        Assert.Equal("visitor@example.com", Assert.Single(mail.ReplyTo.Mailboxes).Address);
        Assert.Empty(mail.Cc); Assert.Empty(mail.Bcc);
        Assert.Equal("Portfolio contact", mail.Subject);
        Assert.Null(mail.HtmlBody);
        Assert.Contains("<script>literal</script>", mail.TextBody);
        Assert.Contains(".line", mail.TextBody);
    }

    private static async Task<(string Recipient, MimeMessage Mail)> ReceiveAsync(TcpListener listener, CancellationToken token)
    {
        using var client = await listener.AcceptTcpClientAsync(token);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
        await writer.WriteLineAsync("220 localhost test SMTP");
        var recipient = "";
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("EHLO", StringComparison.Ordinal)) await writer.WriteLineAsync("250-localhost\r\n250 SIZE 32768");
            else if (line.StartsWith("RCPT TO:", StringComparison.Ordinal)) { recipient = line; await writer.WriteLineAsync("250 OK"); }
            else if (line == "DATA")
            {
                await writer.WriteLineAsync("354 End with dot");
                var text = new StringBuilder();
                while (await reader.ReadLineAsync(token) is { } data && data != ".") text.Append(data.StartsWith("..", StringComparison.Ordinal) ? data[1..] : data).Append("\r\n");
                await writer.WriteLineAsync("250 Accepted");
                return (recipient, MimeMessage.Load(new MemoryStream(Encoding.ASCII.GetBytes(text.ToString()))));
            }
            else await writer.WriteLineAsync("250 OK");
        }
        throw new InvalidOperationException("SMTP client disconnected before DATA.");
    }
}
