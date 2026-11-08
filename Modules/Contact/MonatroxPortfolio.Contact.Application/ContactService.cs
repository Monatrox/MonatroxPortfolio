using MonatroxPortfolio.Contact.Application.Abstractions;
using MonatroxPortfolio.Contact.Domain;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Contact.Application;

public sealed class ContactService(IContactSender sender)
{
    public async Task<Result<ContactReceiptDto>> SubmitAsync(ContactRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(request.Website))
            return Result<ContactReceiptDto>.Failure("contact.invalid", "Unable to submit this message.");
        ContactMessage message;
        try { message = new(request.Name, request.Email, request.Message); }
        catch (ArgumentException ex) { return Result<ContactReceiptDto>.Failure("contact.invalid", ex.Message); }
        return await sender.SendAsync(message, cancellationToken);
    }
}
