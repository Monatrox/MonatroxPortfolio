using MonatroxPortfolio.Profile.Application.Abstractions;
using MonatroxPortfolio.Profile.Application.Features.Content;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Profile.Application.Features.Read;

public sealed class ProfileQueries(IProfileStore store) : IProfileQueries
{
    public async Task<Result<ProfileDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await store.GetAsync(cancellationToken);
        return profile is null
            ? Result<ProfileDto>.Failure("profile.not_found", "Profile content has not been imported.")
            : Result<ProfileDto>.Success(ProfileMapping.ToDto(profile));
    }
}
