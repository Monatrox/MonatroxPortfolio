using MonatroxPortfolio.Contact.Domain;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Contact.Application.Abstractions;

public interface IContactSender
{
    ContactDeliveryInfoDto Info { get; }
    Task<Result<ContactReceiptDto>> SendAsync(ContactMessage message, CancellationToken cancellationToken);
}
