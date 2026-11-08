using System.Net.Mail;

namespace MonatroxPortfolio.Contact.Domain;

public sealed class ContactMessage
{
    public const string Recipient = "zac@monatrox.com";
    public string Name { get; }
    public string Email { get; }
    public string Body { get; }

    public ContactMessage(string? name, string? email, string? body)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name.Any(char.IsControl))
            throw new ArgumentException("Name must contain 1–100 characters, without control characters.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || email.Any(char.IsControl) ||
            !MailAddress.TryCreate(email.Trim(), out var address) || address.Address != email.Trim())
            throw new ArgumentException("Provide a valid email address, without a display name.");
        if (string.IsNullOrWhiteSpace(body) || body.Length > 5000 || body.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            throw new ArgumentException("Message must contain 1–5000 characters, without unsupported control characters.");
        Name = name.Trim(); Email = address.Address; Body = body.Trim();
    }
}
