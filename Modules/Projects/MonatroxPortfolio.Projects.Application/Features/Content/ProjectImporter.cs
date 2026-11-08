using System.Text.Json;
using MonatroxPortfolio.Projects.Application.Abstractions;
using MonatroxPortfolio.Projects.Domain;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Projects.Application.Features.Content;

public sealed class ProjectImporter(IProjectStore store) : IContentImporter
{
    public string FileName => "projects.json";
    public string? AdditionalDirectory => "project-pages";
    private sealed record ImportBatch(PortfolioProject[] Projects, ProjectPage[] Pages);

    private static Result<ImportBatch> Parse(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<ProjectImportDto[]>(json, ContentJson.Options)
                ?? throw new ArgumentException("Projects content cannot be null.");
            var projects = new List<PortfolioProject>();
            var pages = new List<ProjectPage>();
            foreach (var item in data)
            {
                ArgumentNullException.ThrowIfNull(item);
                projects.Add(new(item.Id, item.Name, item.Description, item.ThumbnailSrc, item.Link, item.SortOrder));
                if (item.Page is not null)
                {
                    ProjectDocumentValidation.Validate(item.Page);
                    pages.Add(new(item.Id, JsonSerializer.Serialize(item.Page, ContentJson.Options)));
                }
            }
            if (projects.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != projects.Count)
                throw new ArgumentException("Project IDs must be unique across all project files.");
            return Result<ImportBatch>.Success(new(projects.ToArray(), pages.ToArray()));
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return Result<ImportBatch>.Failure("projects.invalid_content", ex.Message);
        }
    }

    public Result<int> Validate(string json)
    {
        var result = Parse(json);
        return result.IsSuccess ? Result<int>.Success(result.Value!.Projects.Length) : Result<int>.Failure(result.Error!.Code, result.Error.Message);
    }

    public async Task<Result<int>> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        var result = Parse(json);
        if (!result.IsSuccess) return Result<int>.Failure(result.Error!.Code, result.Error.Message);
        await store.StageAsync(result.Value!.Projects, cancellationToken);
        await store.StagePagesAsync(result.Value.Pages, cancellationToken);
        await store.CommitAsync(cancellationToken);
        return Result<int>.Success(result.Value.Projects.Length);
    }
}
