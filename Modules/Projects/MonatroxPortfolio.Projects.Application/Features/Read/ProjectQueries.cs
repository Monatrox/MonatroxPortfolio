using System.Text.Json;
using MonatroxPortfolio.Projects.Application.Abstractions;
using MonatroxPortfolio.Projects.Domain;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Projects.Application.Features.Read;

public sealed class ProjectQueries(IProjectStore store) : IProjectQueries
{
    private static ProjectDto ToDto(PortfolioProject project) => new(project.Id, project.Name, project.Description, project.ThumbnailSrc, project.Link, project.SortOrder);

    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await store.ListAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<Result<ProjectPageDto>> GetPageAsync(string id, CancellationToken cancellationToken = default)
    {
        var page = await store.GetPageAsync(id, cancellationToken);
        return page is null
            ? Result<ProjectPageDto>.Failure("projects.page_not_found", "Project page was not found.")
            : Result<ProjectPageDto>.Success(new(id, JsonSerializer.Deserialize<ProjectDocumentDto>(page.DocumentJson, ContentJson.Options)
                ?? throw new InvalidDataException("Stored project document was empty.")));
    }

    public async Task<Result<ProjectDto>> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var project = await store.GetAsync(id, cancellationToken);
        return project is null
            ? Result<ProjectDto>.Failure("projects.not_found", "Project was not found.")
            : Result<ProjectDto>.Success(ToDto(project));
    }
}
