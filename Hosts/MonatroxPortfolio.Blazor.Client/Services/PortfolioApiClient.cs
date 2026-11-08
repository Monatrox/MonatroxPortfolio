using System.Net.Http.Json;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Projects.Public;

namespace MonatroxPortfolio.Blazor.Client.Services;

public sealed class PortfolioApiClient(HttpClient http)
{
    public async Task<ContactDeliveryInfoDto> GetContactInfoAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ContactDeliveryInfoDto>("/api/contact", cancellationToken)
        ?? throw new InvalidDataException("API returned empty contact status.");

    public async Task<ContactReceiptDto> SendContactAsync(ContactRequestDto contact, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/contact") { Content = JsonContent.Create(contact) };
        request.Headers.Add("X-Portfolio-Contact", "1");
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContactReceiptDto>(cancellationToken)
            ?? throw new InvalidDataException("API returned empty contact receipt.");
    }

    public async Task<ProfileDto> GetProfileAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ProfileDto>("/api/profile", cancellationToken)
        ?? throw new InvalidDataException("API returned empty profile.");

    public async Task<ProjectPageDto> GetProjectPageAsync(string id, CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ProjectPageDto>($"/api/projects/{Uri.EscapeDataString(id)}/page", cancellationToken)
        ?? throw new InvalidDataException("API returned empty project page.");

    public async Task<ProjectDto[]> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ProjectDto[]>("/api/projects", cancellationToken)
        ?? throw new InvalidDataException("API returned empty projects response.");
}
