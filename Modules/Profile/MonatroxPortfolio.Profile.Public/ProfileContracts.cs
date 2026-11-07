using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Profile.Public;

public sealed record ProfileDto(string Alias, string Name, string Headline, string Biography,
    SocialLinkDto[] SocialLinks, SkillDto[] Skills, CareerEntryDto[] Career, EducationEntryDto[] Education);
public sealed record SocialLinkDto(string Id, string Name, string Url, int SortOrder);
public sealed record SkillDto(string Id, string Name, string Category, int SortOrder);
public sealed record CareerEntryDto(string Id, string Period, string Roles, string Organization, string Location, int SortOrder);
public sealed record EducationEntryDto(string Id, string Qualifications, string Institution, string Location, int SortOrder);

public interface IProfileQueries
{
    Task<Result<ProfileDto>> GetAsync(CancellationToken cancellationToken = default);
}
