using MonatroxPortfolio.Projects.Domain;

namespace MonatroxPortfolio.Projects.Application.Abstractions;

public interface IProjectStore
{
    Task<IReadOnlyList<PortfolioProject>> ListAsync(CancellationToken cancellationToken);
    Task<PortfolioProject?> GetAsync(string id, CancellationToken cancellationToken);
    Task StageAsync(IReadOnlyList<PortfolioProject> projects, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
