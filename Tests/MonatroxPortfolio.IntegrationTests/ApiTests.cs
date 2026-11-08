using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Api;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;
using MonatroxPortfolio.Testing;

namespace MonatroxPortfolio.IntegrationTests;

public sealed class ApiTests
{
    private sealed class ApiFactory(string connection) : WebApplicationFactory<ApiAssemblyMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Test")
            .UseSetting("ConnectionStrings:Portfolio", connection);
    }

    [Fact]
    public async Task Two_modules_share_file_without_sharing_tables_or_migration_history()
    {
        using var temp = new TemporaryDirectory();
        await using var factory = new ApiFactory(temp.ConnectionString);
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            foreach (var migration in scope.ServiceProvider.GetServices<IModuleMigration>()) await migration.MigrateAsync();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/profile")).StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<ProjectDto[]>("/api/projects"))!);
            foreach (var importer in scope.ServiceProvider.GetServices<IContentImporter>())
            {
                var json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "content", importer.FileName));
                if (importer.AdditionalDirectory is { } directory)
                {
                    var documents = JsonSerializer.Deserialize<List<JsonElement>>(json)!;
                    foreach (var file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "content", directory), "*.json"))
                        documents.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(file)));
                    json = JsonSerializer.Serialize(documents);
                }
                Assert.True((await importer.ImportAsync(json)).IsSuccess);
            }
            foreach (var migration in scope.ServiceProvider.GetServices<IModuleMigration>()) await migration.MigrateAsync();
        }
        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/profile");
        Assert.Equal("Monatrox", profile!.Alias);
        Assert.Equal(44, profile.Skills.Length);
        var projects = await client.GetFromJsonAsync<ProjectDto[]>("/api/projects");
        Assert.NotNull(projects);
        Assert.Equal(new[] { "ccf", "distill", "portfolio" }, projects.Select(x => x.Id));
        Assert.Equal(projects[0], await client.GetFromJsonAsync<ProjectDto>($"/api/projects/{projects[0].Id}"));
        var missing = await client.GetAsync("/api/projects/missing");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/projects", projects[0])).StatusCode);
        await using var connection = new SqliteConnection(temp.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('__ProfileMigrations','__ProjectsMigrations')";
        Assert.Equal(2L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT COUNT(*) FROM Profile_Skills";
        Assert.Equal(44L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT COUNT(*) FROM Projects_Projects";
        Assert.Equal(3L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Case_study_endpoint_is_additive_read_only_and_keeps_body_out_of_listings()
    {
        using var temp = new TemporaryDirectory();
        await using var factory = new ApiFactory(temp.ConnectionString);
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        foreach (var migration in scope.ServiceProvider.GetServices<IModuleMigration>()) await migration.MigrateAsync();
        var json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "content/project-pages/ccf.json"));
        var importer = scope.ServiceProvider.GetServices<IContentImporter>().Single(x => x.FileName == "projects.json");
        Assert.True((await importer.ImportAsync("[" + json + "]")).IsSuccess);
        var page = await client.GetFromJsonAsync<ProjectPageDto>("/api/projects/ccf/page");
        Assert.Equal("CCF", page!.Document.Hero.Title);
        Assert.Equal(5, page.Document.Sections.Length);
        Assert.DoesNotContain("schemaVersion", await client.GetStringAsync("/api/projects"));
        Assert.Equal("CCF", (await client.GetFromJsonAsync<ProjectDto>("/api/projects/ccf"))!.Name);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/projects/missing/page")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/projects/ccf/page", page)).StatusCode);
    }

    [Fact]
    public async Task Api_startup_does_not_create_or_seed_database()
    {
        using var temp = new TemporaryDirectory();
        await using var factory = new ApiFactory(temp.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.False(File.Exists(Path.Combine(temp.Path, "portfolio.db")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_command_validates_every_module_before_writes(bool invalidPage)
    {
        using var temp = new TemporaryDirectory();
        var root = FindRoot();
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var assembly = Path.Combine(root, "Hosts/MonatroxPortfolio.Migrations/bin", config, "net10.0/MonatroxPortfolio.Migrations.dll");
        Assert.True(File.Exists(assembly));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "content/profile.json"), Path.Combine(temp.Path, "profile.json"));
        if (invalidPage)
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "content/projects.json"), Path.Combine(temp.Path, "projects.json"));
            Directory.CreateDirectory(Path.Combine(temp.Path, "project-pages"));
            await File.WriteAllTextAsync(Path.Combine(temp.Path, "project-pages/bad.json"), "{}");
        }
        else await File.WriteAllTextAsync(Path.Combine(temp.Path, "projects.json"), "[{}]");
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("--import");
        start.ArgumentList.Add(temp.Path);
        start.Environment["ConnectionStrings__Portfolio"] = temp.ConnectionString;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(deadline.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        Assert.Equal(1, process.ExitCode);
        Assert.Contains("projects.json", await stderr);
        Assert.DoesNotContain("Imported profile.json", await stdout);
        Assert.False(File.Exists(Path.Combine(temp.Path, "portfolio.db")));
    }

    [Fact]
    public async Task Validation_command_reads_project_files_without_opening_database()
    {
        using var temp = new TemporaryDirectory();
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var assembly = Path.Combine(FindRoot(), "Hosts/MonatroxPortfolio.Migrations/bin", config, "net10.0/MonatroxPortfolio.Migrations.dll");
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("--validate");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "content"));
        start.Environment["ConnectionStrings__Portfolio"] = temp.ConnectionString;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(deadline.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        Assert.True(process.ExitCode == 0, await stderr);
        Assert.Contains("Content validated", await stdout);
        Assert.False(File.Exists(Path.Combine(temp.Path, "portfolio.db")));
    }

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MonatroxPortfolio.slnx"))) return dir.FullName;
        throw new InvalidOperationException("Solution root not found.");
    }
}
