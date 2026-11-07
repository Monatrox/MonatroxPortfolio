using System.Text.Json;
using MonatroxPortfolio.Profile.Application.Abstractions;
using MonatroxPortfolio.Profile.Domain;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Profile.Application.Features.Content;

public sealed class ProfileImporter(IProfileStore store) : IContentImporter
{
    public string FileName => "profile.json";

    private static Result<PortfolioProfile> Parse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<ProfileDto>(json, ContentJson.Options)
                ?? throw new ArgumentException("Profile content cannot be null.");
            return Result<PortfolioProfile>.Success(ProfileMapping.FromDto(dto));
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return Result<PortfolioProfile>.Failure("profile.invalid_content", ex.Message);
        }
    }

    public Result<int> Validate(string json)
    {
        var result = Parse(json);
        return result.IsSuccess ? Result<int>.Success(1) : Result<int>.Failure(result.Error!.Code, result.Error.Message);
    }

    public async Task<Result<int>> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        var result = Parse(json);
        if (!result.IsSuccess) return Result<int>.Failure(result.Error!.Code, result.Error.Message);
        await store.StageAsync(result.Value!, cancellationToken);
        await store.CommitAsync(cancellationToken);
        return Result<int>.Success(1);
    }
}
