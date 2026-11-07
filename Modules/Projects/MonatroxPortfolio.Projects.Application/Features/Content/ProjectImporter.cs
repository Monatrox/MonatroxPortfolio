using System.Text.Json;
using MonatroxPortfolio.Projects.Application.Abstractions;
using MonatroxPortfolio.Projects.Domain;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Projects.Application.Features.Content;

public sealed class ProjectImporter(IProjectStore store) : IContentImporter
{
    public string FileName => "projects.json";

    private static Result<PortfolioProject[]> Parse(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<ProjectDto[]>(json, ContentJson.Options)
                ?? throw new ArgumentException("Projects content cannot be null.");
            var projects = data.Select(x =>
            {
                ArgumentNullException.ThrowIfNull(x);
                return new PortfolioProject(x.Id, x.Name, x.Description, x.ThumbnailSrc, x.Link, x.SortOrder);
            }).ToArray();
            if (projects.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != projects.Length)
                throw new ArgumentException("Project IDs must be unique.");
            return Result<PortfolioProject[]>.Success(projects);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return Result<PortfolioProject[]>.Failure("projects.invalid_content", ex.Message);
        }
    }

    public Result<int> Validate(string json)
    {
        var result = Parse(json);
        return result.IsSuccess ? Result<int>.Success(result.Value!.Length) : Result<int>.Failure(result.Error!.Code, result.Error.Message);
    }

    public async Task<Result<int>> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        var result = Parse(json);
        if (!result.IsSuccess) return Result<int>.Failure(result.Error!.Code, result.Error.Message);
        await store.StageAsync(result.Value!, cancellationToken);
        await store.CommitAsync(cancellationToken);
        return Result<int>.Success(result.Value!.Length);
    }
}
