using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Projects.Application.Abstractions;
using MonatroxPortfolio.Projects.Application.Features.Content;
using MonatroxPortfolio.Projects.Application.Features.Read;
using MonatroxPortfolio.Projects.Infrastructure.Persistence;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Projects.Infrastructure;

public static class Registration
{
    public static IServiceCollection AddProjects(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProjectsDbContext>(options => options.UseSqlite(connectionString,
            sqlite => sqlite.MigrationsHistoryTable("__ProjectsMigrations")));
        services.AddScoped<IProjectStore, ProjectStore>();
        services.AddScoped<IProjectQueries, ProjectQueries>();
        services.AddScoped<IContentImporter, ProjectImporter>();
        services.AddScoped<IModuleMigration, ProjectsMigration>();
        return services;
    }
}

internal sealed class ProjectsMigration(ProjectsDbContext db) : IModuleMigration
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) => db.Database.MigrateAsync(cancellationToken);
}
