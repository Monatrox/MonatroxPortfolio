using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MonatroxPortfolio.Profile.Domain;

namespace MonatroxPortfolio.Profile.Infrastructure.Persistence;

public sealed class ProfileDbContext(DbContextOptions<ProfileDbContext> options) : DbContext(options)
{
    public DbSet<PortfolioProfile> Profiles => Set<PortfolioProfile>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var profile = model.Entity<PortfolioProfile>();
        profile.ToTable("Profile_Profiles").HasKey(x => x.Id);
        profile.OwnsMany(x => x.SocialLinks, owned =>
        {
            owned.ToTable("Profile_SocialLinks");
            owned.WithOwner().HasForeignKey("ProfileId");
            owned.HasKey("ProfileId", "Id");
        });
        profile.OwnsMany(x => x.Skills, owned =>
        {
            owned.ToTable("Profile_Skills");
            owned.WithOwner().HasForeignKey("ProfileId");
            owned.HasKey("ProfileId", "Id");
        });
        profile.OwnsMany(x => x.Career, owned =>
        {
            owned.ToTable("Profile_Career");
            owned.WithOwner().HasForeignKey("ProfileId");
            owned.HasKey("ProfileId", "Id");
        });
        profile.OwnsMany(x => x.Education, owned =>
        {
            owned.ToTable("Profile_Education");
            owned.WithOwner().HasForeignKey("ProfileId");
            owned.HasKey("ProfileId", "Id");
        });
        profile.Navigation(x => x.SocialLinks).UsePropertyAccessMode(PropertyAccessMode.Field);
        profile.Navigation(x => x.Skills).UsePropertyAccessMode(PropertyAccessMode.Field);
        profile.Navigation(x => x.Career).UsePropertyAccessMode(PropertyAccessMode.Field);
        profile.Navigation(x => x.Education).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Portfolio")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Portfolio to an explicit SQLite connection string.");
        return new(new DbContextOptionsBuilder<ProfileDbContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsHistoryTable("__ProfileMigrations")).Options);
    }
}
