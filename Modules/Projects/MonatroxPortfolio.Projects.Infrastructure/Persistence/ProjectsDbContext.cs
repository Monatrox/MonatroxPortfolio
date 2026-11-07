using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MonatroxPortfolio.Projects.Domain;

namespace MonatroxPortfolio.Projects.Infrastructure.Persistence;

public sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : DbContext(options)
{
    public DbSet<PortfolioProject> Projects => Set<PortfolioProject>();

    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<PortfolioProject>().ToTable("Projects_Projects").HasKey(x => x.Id);
}

public sealed class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    public ProjectsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Portfolio")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Portfolio to an explicit SQLite connection string.");
        return new(new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsHistoryTable("__ProjectsMigrations")).Options);
    }
}
