using Microsoft.EntityFrameworkCore;
using MonatroxPortfolio.Projects.Application.Abstractions;
using MonatroxPortfolio.Projects.Domain;

namespace MonatroxPortfolio.Projects.Infrastructure.Persistence;

public sealed class ProjectStore(ProjectsDbContext db) : IProjectStore
{
    public async Task<IReadOnlyList<PortfolioProject>> ListAsync(CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<PortfolioProject?> GetAsync(string id, CancellationToken cancellationToken) =>
        db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task StageAsync(IReadOnlyList<PortfolioProject> projects, CancellationToken cancellationToken)
    {
        var ids = projects.Select(x => x.Id).ToArray();
        var existing = await db.Projects.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (var project in projects)
        {
            if (existing.TryGetValue(project.Id, out var current)) db.Entry(current).CurrentValues.SetValues(project);
            else db.Projects.Add(project);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
