using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Projects.Domain;
using MonatroxPortfolio.Projects.Infrastructure;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;
using MonatroxPortfolio.Testing;

namespace MonatroxPortfolio.Projects.Tests;

public sealed class ProjectTests
{
    private static readonly string Json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "projects.json"));

    [Fact]
    public async Task Repeat_import_updates_by_stable_id_without_deleting_unlisted_projects()
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProjects(temp.ConnectionString).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var migrations = scope.ServiceProvider.GetRequiredService<IModuleMigration>();
        await migrations.MigrateAsync();
        await migrations.MigrateAsync();
        var queries = scope.ServiceProvider.GetRequiredService<IProjectQueries>();
        Assert.Empty(await queries.ListAsync());
        Assert.Equal("projects.not_found", (await queries.GetAsync("missing")).Error!.Code);
        var importer = scope.ServiceProvider.GetRequiredService<IContentImporter>();
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        Assert.True((await importer.ImportAsync(Json)).IsSuccess);
        var projects = await queries.ListAsync();
        Assert.Equal(5, projects.Count);
        var changed = projects[0] with { Name = "Updated project", SortOrder = 99 };
        Assert.True((await importer.ImportAsync(JsonSerializer.Serialize(new[] { changed }, ContentJson.Options))).IsSuccess);
        Assert.Equal(5, (await queries.ListAsync()).Count);
        Assert.Equal(changed, (await queries.GetAsync(changed.Id)).Value);
        Assert.Equal(changed.Id, (await queries.ListAsync())[^1].Id);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[1]")]
    public async Task Invalid_input_is_rejected_without_database_access(string json)
    {
        await using var services = new ServiceCollection().AddProjects("Data Source=:memory:").BuildServiceProvider();
        var importer = services.GetRequiredService<IContentImporter>();
        Assert.False(importer.Validate(json).IsSuccess);
        Assert.False((await importer.ImportAsync(json)).IsSuccess);
    }

    [Fact]
    public async Task Duplicate_ids_and_late_invalid_record_do_not_partially_import()
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProjects(temp.ConnectionString).BuildServiceProvider();
        await services.GetRequiredService<IModuleMigration>().MigrateAsync();
        var importer = services.GetRequiredService<IContentImporter>();
        var projects = JsonSerializer.Deserialize<ProjectDto[]>(Json, ContentJson.Options)!;
        var duplicate = JsonSerializer.Serialize(new[] { projects[0], projects[0] }, ContentJson.Options);
        Assert.False(importer.Validate(duplicate).IsSuccess);
        projects[^1] = projects[^1] with { Name = "" };
        Assert.False((await importer.ImportAsync(JsonSerializer.Serialize(projects, ContentJson.Options))).IsSuccess);
        Assert.Empty(await services.GetRequiredService<IProjectQueries>().ListAsync());
    }

    [Theory]
    [InlineData("/images/project.png")]
    [InlineData("/projects/one?view=details#top")]
    [InlineData("/")]
    [InlineData("images/project.png")]
    [InlineData("https://example.com/project")]
    [InlineData("http://example.com/project")]
    public void Domain_accepts_safe_web_references(string reference)
    {
        var project = new PortfolioProject("id", "name", "description", reference, reference, 0);
        Assert.Equal(reference, project.ThumbnailSrc);
        Assert.Equal(reference, project.Link);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example")]
    [InlineData("file:///etc/passwd")]
    [InlineData("///evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("/images/\nfile.png")]
    [InlineData("data:text/html,hello")]
    public void Domain_rejects_unsafe_links(string link)
    {
        Assert.Throws<ArgumentException>(() => new PortfolioProject("id", "name", "description", "image.png", link, 0));
        Assert.Throws<ArgumentException>(() => new PortfolioProject("id", "name", "description", link, null, 0));
    }
}
