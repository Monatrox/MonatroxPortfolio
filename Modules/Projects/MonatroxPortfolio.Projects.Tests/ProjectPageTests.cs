using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Projects.Infrastructure;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;
using MonatroxPortfolio.Testing;

namespace MonatroxPortfolio.Projects.Tests;

public sealed class ProjectPageTests
{
    private static ProjectImportDto Read(string id) => JsonSerializer.Deserialize<ProjectImportDto>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "project-pages", id + ".json")), ContentJson.Options)!;
    private static string Json(params ProjectImportDto[] items) => JsonSerializer.Serialize(items, ContentJson.Options);

    [Fact]
    public void Distill_is_self_contained_with_only_a_repository_root_link()
    {
        var json = Json(Read("distill"));
        Assert.DoesNotContain("/blob/", json);
        Assert.DoesNotContain("/tree/", json);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(json, @"https://github\.com/Monatrox/distill"));
        Assert.Contains("connection_string_env", json);
        Assert.Contains("not a live CI status or production guarantee", json);
    }

    [Theory]
    [InlineData("ccf")]
    [InlineData("distill")]
    [InlineData("portfolio")]
    public async Task Documents_roundtrip_repeat_safely_without_bloating_or_breaking_summary_contract(string id)
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProjects(temp.ConnectionString).BuildServiceProvider();
        await services.GetRequiredService<IModuleMigration>().MigrateAsync();
        var importer = services.GetRequiredService<IContentImporter>();
        var queries = services.GetRequiredService<IProjectQueries>();
        var item = Read(id);
        Assert.True(importer.Validate(Json(item)).IsSuccess);
        Assert.True((await importer.ImportAsync(Json(item))).IsSuccess);
        Assert.True((await importer.ImportAsync(Json(item))).IsSuccess);
        Assert.Single(await queries.ListAsync());
        var page = (await queries.GetPageAsync(id)).Value!;
        Assert.Equal(id, page.ProjectId);
        Assert.Equal(JsonSerializer.Serialize(item.Page, ContentJson.Options), JsonSerializer.Serialize(page.Document, ContentJson.Options));
        Assert.Equal(5, page.Document.Sections.Length);
        Assert.DoesNotContain("schemaVersion", JsonSerializer.Serialize(await queries.ListAsync(), ContentJson.Options));
        Assert.True((await importer.ImportAsync(Json(item with { Name = "Updated", Page = null }))).IsSuccess);
        Assert.Equal("Updated", (await queries.GetAsync(id)).Value!.Name);
        Assert.True((await queries.GetPageAsync(id)).IsSuccess);
        Assert.False((await queries.GetPageAsync("missing")).IsSuccess);
    }

    [Theory]
    [InlineData("[click](javascript:alert%281%29)")]
    [InlineData("[click](data:text/html,hello)")]
    [InlineData("[click](//evil.example)")]
    [InlineData("[click](file:///etc/passwd)")]
    public void Unsafe_markdown_links_are_rejected_without_storage(string markdown)
    {
        using var services = new ServiceCollection().AddProjects("Data Source=:memory:").BuildServiceProvider();
        var item = Read("ccf");
        var changed = item with { Page = item.Page! with { Notes = [markdown] } };
        Assert.False(services.GetRequiredService<IContentImporter>().Validate(Json(changed)).IsSuccess);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("duplicate-anchor")]
    [InlineData("table")]
    [InlineData("calculator")]
    [InlineData("columns")]
    [InlineData("nesting")]
    public void Invalid_document_shapes_are_rejected(string scenario)
    {
        using var services = new ServiceCollection().AddProjects("Data Source=:memory:").BuildServiceProvider();
        var item = Read("ccf");
        var page = item.Page!;
        ProjectBlockDto block = new MarkdownBlockDto("Text");
        switch (scenario)
        {
            case "version": page = page with { SchemaVersion = 2 }; break;
            case "duplicate-anchor": page = page with { Sections = [page.Sections[0], page.Sections[0]] }; break;
            case "table": block = new TableBlockDto("Table", ["One", "Two"], [["Only one"]]); break;
            case "calculator": block = new CalculatorBlockDto("Model, not benchmark", WindowSeconds: 0); break;
            case "columns": block = new ColumnsBlockDto([block], 4); break;
            case "nesting": for (var i = 0; i < 9; i++) block = new PanelBlockDto([block]); break;
        }
        if (scenario is not ("version" or "duplicate-anchor")) page = page with { Introduction = [block] };
        Assert.False(services.GetRequiredService<IContentImporter>().Validate(Json(item with { Page = page })).IsSuccess);
    }

    [Fact]
    public async Task Late_invalid_document_does_not_partially_import_metadata_or_pages()
    {
        using var temp = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddProjects(temp.ConnectionString).BuildServiceProvider();
        await services.GetRequiredService<IModuleMigration>().MigrateAsync();
        var good = Read("ccf");
        var bad = Read("distill");
        bad = bad with { Page = bad.Page! with { Notes = ["[bad](javascript:alert%281%29)"] } };
        Assert.False((await services.GetRequiredService<IContentImporter>().ImportAsync(Json(good, bad))).IsSuccess);
        Assert.Empty(await services.GetRequiredService<IProjectQueries>().ListAsync());
        Assert.False((await services.GetRequiredService<IProjectQueries>().GetPageAsync("ccf")).IsSuccess);
    }

    [Fact]
    public void Unknown_block_types_are_rejected_but_code_is_literal_content()
    {
        using var services = new ServiceCollection().AddProjects("Data Source=:memory:").BuildServiceProvider();
        var item = Read("ccf");
        item = item with { Page = item.Page! with { Introduction = [new CodeBlockDto("Literal", "text", "<script>alert('literal')</script>")] } };
        var json = Json(item);
        Assert.True(services.GetRequiredService<IContentImporter>().Validate(json).IsSuccess);
        Assert.False(services.GetRequiredService<IContentImporter>().Validate(json.Replace("\"type\":\"code\"", "\"type\":\"execute\"", StringComparison.Ordinal)).IsSuccess);
    }
}
