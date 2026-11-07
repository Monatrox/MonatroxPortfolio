using System.Net.Http.Json;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Projects.Public;

namespace MonatroxPortfolio.BlazorServer.Services;

public sealed class PortfolioApiClient(HttpClient http)
{
    public async Task<ProfileDto> GetProfileAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ProfileDto>("/api/profile", cancellationToken)
        ?? throw new InvalidDataException("API returned empty profile.");

    public async Task<ProjectDto[]> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ProjectDto[]>("/api/projects", cancellationToken)
        ?? throw new InvalidDataException("API returned empty projects response.");
}
