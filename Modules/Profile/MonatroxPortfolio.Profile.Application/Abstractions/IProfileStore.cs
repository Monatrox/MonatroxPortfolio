using MonatroxPortfolio.Profile.Domain;

namespace MonatroxPortfolio.Profile.Application.Abstractions;

public interface IProfileStore
{
    Task<PortfolioProfile?> GetAsync(CancellationToken cancellationToken);
    Task StageAsync(PortfolioProfile profile, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
