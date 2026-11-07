using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Projects.Public;

public sealed record ProjectDto(string Id, string Name, string Description, string ThumbnailSrc, string? Link, int SortOrder);

public interface IProjectQueries
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> GetAsync(string id, CancellationToken cancellationToken = default);
}
