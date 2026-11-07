using Microsoft.EntityFrameworkCore;
using MonatroxPortfolio.Profile.Application.Abstractions;
using MonatroxPortfolio.Profile.Domain;

namespace MonatroxPortfolio.Profile.Infrastructure.Persistence;

public sealed class ProfileStore(ProfileDbContext db) : IProfileStore
{
    public Task<PortfolioProfile?> GetAsync(CancellationToken cancellationToken) =>
        db.Profiles.AsNoTracking().AsSplitQuery().SingleOrDefaultAsync(x => x.Id == "main", cancellationToken);

    public async Task StageAsync(PortfolioProfile profile, CancellationToken cancellationToken)
    {
        var existing = await db.Profiles.AsSplitQuery().SingleOrDefaultAsync(x => x.Id == profile.Id, cancellationToken);
        if (existing is null)
        {
            db.Profiles.Add(profile);
            return;
        }
        db.Entry(existing).CurrentValues.SetValues(profile);
        Merge(existing.SocialLinks, profile.SocialLinks, x => x.Id, existing.AddSocialLink);
        Merge(existing.Skills, profile.Skills, x => x.Id, existing.AddSkill);
        Merge(existing.Career, profile.Career, x => x.Id, existing.AddCareerEntry);
        Merge(existing.Education, profile.Education, x => x.Id, existing.AddEducationEntry);
    }

    private void Merge<T>(IEnumerable<T> existing, IEnumerable<T> incoming, Func<T, string> id, Action<T> add) where T : class
    {
        var byId = existing.ToDictionary(id, StringComparer.Ordinal);
        foreach (var item in incoming)
        {
            if (byId.TryGetValue(id(item), out var current)) db.Entry(current).CurrentValues.SetValues(item);
            else add(item);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
