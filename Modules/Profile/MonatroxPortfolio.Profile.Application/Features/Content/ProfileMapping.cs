using MonatroxPortfolio.Profile.Domain;
using MonatroxPortfolio.Profile.Public;

namespace MonatroxPortfolio.Profile.Application.Features.Content;

internal static class ProfileMapping
{
    public static PortfolioProfile FromDto(ProfileDto dto)
    {
        var profile = new PortfolioProfile(dto.Alias, dto.Name, dto.Headline, dto.Biography);
        ArgumentNullException.ThrowIfNull(dto.SocialLinks);
        ArgumentNullException.ThrowIfNull(dto.Skills);
        ArgumentNullException.ThrowIfNull(dto.Career);
        ArgumentNullException.ThrowIfNull(dto.Education);
        foreach (var x in dto.SocialLinks)
        {
            ArgumentNullException.ThrowIfNull(x);
            profile.AddSocialLink(new(x.Id, x.Name, x.Url, x.SortOrder));
        }
        foreach (var x in dto.Skills)
        {
            ArgumentNullException.ThrowIfNull(x);
            profile.AddSkill(new(x.Id, x.Name, x.Category, x.SortOrder));
        }
        foreach (var x in dto.Career)
        {
            ArgumentNullException.ThrowIfNull(x);
            profile.AddCareerEntry(new(x.Id, x.Period, x.Roles, x.Organization, x.Location, x.SortOrder));
        }
        foreach (var x in dto.Education)
        {
            ArgumentNullException.ThrowIfNull(x);
            profile.AddEducationEntry(new(x.Id, x.Qualifications, x.Institution, x.Location, x.SortOrder));
        }
        return profile;
    }

    public static ProfileDto ToDto(PortfolioProfile profile) => new(profile.Alias, profile.Name, profile.Headline, profile.Biography,
        profile.SocialLinks.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new SocialLinkDto(x.Id, x.Name, x.Url, x.SortOrder)).ToArray(),
        profile.Skills.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new SkillDto(x.Id, x.Name, x.Category, x.SortOrder)).ToArray(),
        profile.Career.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new CareerEntryDto(x.Id, x.Period, x.Roles, x.Organization, x.Location, x.SortOrder)).ToArray(),
        profile.Education.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new EducationEntryDto(x.Id, x.Qualifications, x.Institution, x.Location, x.SortOrder)).ToArray());
}
