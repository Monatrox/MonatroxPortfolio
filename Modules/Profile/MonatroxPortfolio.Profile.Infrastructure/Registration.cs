using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Profile.Application.Abstractions;
using MonatroxPortfolio.Profile.Application.Features.Content;
using MonatroxPortfolio.Profile.Application.Features.Read;
using MonatroxPortfolio.Profile.Infrastructure.Persistence;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Profile.Infrastructure;

public static class Registration
{
    public static IServiceCollection AddProfile(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProfileDbContext>(options => options.UseSqlite(connectionString,
            sqlite => sqlite.MigrationsHistoryTable("__ProfileMigrations")));
        services.AddScoped<IProfileStore, ProfileStore>();
        services.AddScoped<IProfileQueries, ProfileQueries>();
        services.AddScoped<IContentImporter, ProfileImporter>();
        services.AddScoped<IModuleMigration, ProfileMigration>();
        return services;
    }
}

internal sealed class ProfileMigration(ProfileDbContext db) : IModuleMigration
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) => db.Database.MigrateAsync(cancellationToken);
}
