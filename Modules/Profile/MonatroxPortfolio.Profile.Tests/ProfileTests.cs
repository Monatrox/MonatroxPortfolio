using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Profile.Domain;
using MonatroxPortfolio.Profile.Infrastructure;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Shared.Contracts;
using MonatroxPortfolio.Testing;

namespace MonatroxPortfolio.Profile.Tests;

public sealed class ProfileTests
{
    private static readonly string Json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "profile.json"));

    [Fact]
    public async Task Migration_and_repeat_import_preserve_unlisted_items_and_update_matching_ids()
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProfile(temp.ConnectionString).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var migrations = scope.ServiceProvider.GetRequiredService<IModuleMigration>();
        await migrations.MigrateAsync();
        await migrations.MigrateAsync();
        var queries = scope.ServiceProvider.GetRequiredService<IProfileQueries>();
        Assert.Equal("profile.not_found", (await queries.GetAsync()).Error!.Code);
        var importer = scope.ServiceProvider.GetRequiredService<IContentImporter>();
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        var initial = (await queries.GetAsync()).Value!;
        Assert.Equal(81, initial.Skills.Length);
        Assert.Equal(new[] { "C#", "C++", "Python", "SQL", "JavaScript", "TypeScript", "Bash", "Rust" }, initial.Skills.Where(skill => skill.Category == "Languages").Select(skill => skill.Name));
        Assert.Equal("Leadership", initial.Skills.Single(skill => skill.Id == "code-review").Category);
        Assert.Contains(initial.Skills, skill => skill.Id == "bruno" && skill.Name == "Bruno");
        Assert.Equal(9, initial.Skills.Select(skill => skill.Category).Distinct().Count());
        Assert.Equal("Web", initial.Skills.Single(skill => skill.Id == "aspnet").Category);
        Assert.Equal("Leadership", initial.Skills.Single(skill => skill.Id == "project-management").Category);
        Assert.Equal("Valkey / Redis", initial.Skills.Single(skill => skill.Id == "redis").Name);
        Assert.Equal("Data", initial.Skills.Single(skill => skill.Id == "sql-server").Category);
        Assert.Equal(3, initial.SocialLinks.Length);
        Assert.Equal(3, initial.Career.Length);
        Assert.Equal(2, initial.Education.Length);
        var changed = initial with
        {
            Headline = "Updated headline",
            Skills = [initial.Skills[0] with { Name = "Updated tool" }],
            SocialLinks = [], Career = [], Education = []
        };
        Assert.True((await importer.ImportAsync(JsonSerializer.Serialize(changed, ContentJson.Options))).IsSuccess);
        var updated = (await queries.GetAsync()).Value!;
        Assert.Equal("Updated headline", updated.Headline);
        Assert.Equal(81, updated.Skills.Length);
        Assert.Contains(updated.Skills, x => x.Id == initial.Skills[0].Id && x.Name == "Updated tool");
        Assert.Equal(3, updated.SocialLinks.Length);
        Assert.Equal(3, updated.Career.Length);
        Assert.Equal(2, updated.Education.Length);

        var additions = updated with
        {
            SocialLinks = [new("extra-link", "Extra", "#", 99)],
            Skills = [new("extra-skill", "Extra", "Tools", 99)],
            Career = [new("extra-career", "2026", "Developer", "Company", "Location", 99)],
            Education = [new("extra-education", "Degree", "College", "Location", 99)]
        };
        var additionsJson = JsonSerializer.Serialize(additions, ContentJson.Options);
        Assert.True((await importer.ImportAsync(additionsJson)).IsSuccess);
        Assert.True((await importer.ImportAsync(additionsJson)).IsSuccess);
        var expanded = (await queries.GetAsync()).Value!;
        Assert.Equal(4, expanded.SocialLinks.Length);
        Assert.Equal(82, expanded.Skills.Length);
        Assert.Equal(4, expanded.Career.Length);
        Assert.Equal(3, expanded.Education.Length);
    }

    [Theory]
    [InlineData("Web")]
    [InlineData("Diagnostics")]
    [InlineData("Leadership")]
    [InlineData("DotNet")]
    [InlineData("Languages")]
    [InlineData("Architecture")]
    [InlineData("Data")]
    [InlineData("Platforms")]
    [InlineData("Workflow")]
    [InlineData("Business")]
    [InlineData("AI")]
    [InlineData("Games")]
    [InlineData("Tools")]
    [InlineData("Skills")]
    public void Skill_categories_accept_grouped_and_legacy_content(string category)
    {
        Assert.Equal(category, new Skill("id", "Name", category, 0).Category);
    }

    [Fact]
    public async Task Skills_refresh_updates_legacy_categories_without_retiring_unlisted_records()
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProfile(temp.ConnectionString).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IModuleMigration>().MigrateAsync();
        var importer = scope.ServiceProvider.GetRequiredService<IContentImporter>();
        var content = JsonSerializer.Deserialize<ProfileDto>(Json, ContentJson.Options)!;
        var legacy = content with { Skills = [
            new("aspnet", "ASP.NET", "DotNet", 9),
            new("redis", "Redis", "Data", 18),
            new("project-management", "Project Management", "Architecture", 3),
            new("unlisted", "Preserved custom skill", "Tools", 99)
        ] };
        Assert.True((await importer.ImportAsync(JsonSerializer.Serialize(legacy, ContentJson.Options))).IsSuccess);
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        var updated = (await scope.ServiceProvider.GetRequiredService<IProfileQueries>().GetAsync()).Value!;
        Assert.Equal(content.Skills.Length + 1, updated.Skills.Length);
        Assert.Equal("Web", updated.Skills.Single(skill => skill.Id == "aspnet").Category);
        Assert.Equal("Valkey / Redis", updated.Skills.Single(skill => skill.Id == "redis").Name);
        Assert.Equal("Leadership", updated.Skills.Single(skill => skill.Id == "project-management").Category);
        Assert.Contains(updated.Skills, skill => skill.Id == "unlisted" && skill.Name == "Preserved custom skill");
    }

    [Fact]
    public void Domain_rejects_duplicate_ids_and_null_items_without_separate_validation()
    {
        var profile = new PortfolioProfile("Alias", "Name", "Headline", "Biography");
        var social = new SocialLink("same", "Link", "#", 0);
        var skill = new Skill("same", "Skill", "Tools", 0);
        var career = new CareerEntry("same", "2020 - Present", "Developer", "Company", "Location", 0);
        var education = new EducationEntry("same", "Degree", "College", "Location", 0);
        profile.AddSocialLink(social);
        profile.AddSkill(skill);
        profile.AddCareerEntry(career);
        profile.AddEducationEntry(education);

        Assert.Throws<ArgumentException>(() => profile.AddSocialLink(new("same", "Other", "#", 1)));
        Assert.Throws<ArgumentException>(() => profile.AddSkill(new("same", "Other", "Skills", 1)));
        Assert.Throws<ArgumentException>(() => profile.AddCareerEntry(new("same", "2021", "Other", "Other", "Other", 1)));
        Assert.Throws<ArgumentException>(() => profile.AddEducationEntry(new("same", "Other", "Other", "Other", 1)));
        Assert.Throws<ArgumentNullException>(() => profile.AddSocialLink(null!));
        Assert.Throws<ArgumentNullException>(() => profile.AddSkill(null!));
        Assert.Throws<ArgumentNullException>(() => profile.AddCareerEntry(null!));
        Assert.Throws<ArgumentNullException>(() => profile.AddEducationEntry(null!));
        Assert.Same(social, Assert.Single(profile.SocialLinks));
        Assert.Same(skill, Assert.Single(profile.Skills));
        Assert.Same(career, Assert.Single(profile.Career));
        Assert.Same(education, Assert.Single(profile.Education));
    }

    [Fact]
    public void Domain_collections_cannot_be_modified_directly()
    {
        var profile = new PortfolioProfile("Alias", "Name", "Headline", "Biography");
        Assert.True(((ICollection<SocialLink>)profile.SocialLinks).IsReadOnly);
        Assert.True(((ICollection<Skill>)profile.Skills).IsReadOnly);
        Assert.True(((ICollection<CareerEntry>)profile.Career).IsReadOnly);
        Assert.True(((ICollection<EducationEntry>)profile.Education).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((ICollection<Skill>)profile.Skills).Add(new("id", "Skill", "Tools", 0)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"alias\":null}")]
    public async Task Invalid_json_is_rejected_before_database_access(string json)
    {
        await using var services = new ServiceCollection().AddProfile("Data Source=:memory:").BuildServiceProvider();
        var importer = services.GetRequiredService<IContentImporter>();
        Assert.False(importer.Validate(json).IsSuccess);
        Assert.False((await importer.ImportAsync(json)).IsSuccess);
    }

    [Fact]
    public async Task Duplicate_ids_null_items_and_unsafe_urls_are_rejected()
    {
        await using var services = new ServiceCollection().AddProfile("Data Source=:memory:").BuildServiceProvider();
        var importer = services.GetRequiredService<IContentImporter>();
        var profile = JsonSerializer.Deserialize<ProfileDto>(Json, ContentJson.Options)!;
        foreach (var bad in new[]
        {
            profile with { Skills = [profile.Skills[0], profile.Skills[0]] },
            profile with { SocialLinks = [profile.SocialLinks[0] with { Url = "javascript:alert(1)" }] },
            profile with { Education = [null!] }
        }) Assert.False(importer.Validate(JsonSerializer.Serialize(bad, ContentJson.Options)).IsSuccess);
        Assert.Throws<ArgumentException>(() => new Skill("id", "name", "Unknown", 0));
        Assert.Throws<ArgumentException>(() => new PortfolioProfile("", "name", "headline", "bio"));
    }
}
