using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MonatroxPortfolio.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string[] Modules = ["Contact", "Profile", "Projects"];
    private static readonly string[] Layers = ["Application", "Domain", "Infrastructure", "Public", "Tests"];
    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MonatroxPortfolio.slnx"))) return dir.FullName;
        throw new InvalidOperationException("Solution root not found.");
    }
    private static IEnumerable<string> SourceFiles(string directory, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(Root, directory), pattern, SearchOption.AllDirectories)
            .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"));
    private static string[] References(string project) => XDocument.Load(project).Descendants("ProjectReference")
        .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/'))).ToArray();

    private static bool Allowed(string module, string layer, string target)
    {
        var parts = target.Split('.');
        if (target == "MonatroxPortfolio.Shared.Contracts") return true;
        if (parts.Length != 3 || parts[0] != "MonatroxPortfolio" || !Modules.Contains(parts[1])) return false;
        if (parts[1] != module) return layer != "Domain" && parts[2] == "Public";
        return layer switch
        {
            "Application" => parts[2] is "Domain" or "Public",
            "Infrastructure" => parts[2] is "Application" or "Domain" or "Public",
            "Tests" => parts[2] is "Application" or "Domain" or "Infrastructure" or "Public",
            _ => false
        };
    }

    [Fact]
    public void Module_references_respect_layers_and_only_foreign_Public()
    {
        foreach (var project in SourceFiles("Modules", "*.csproj"))
        {
            var parts = Path.GetFileNameWithoutExtension(project).Split('.');
            Assert.Equal(3, parts.Length);
            Assert.Contains(parts[1], Modules);
            Assert.Contains(parts[2], Layers);
            Assert.Equal(parts[1], Directory.GetParent(project)!.Parent!.Name);
            foreach (var target in References(project)) Assert.True(Allowed(parts[1], parts[2], target), $"{project} -> {target}");
        }
    }

    [Theory]
    [InlineData("Profile", "Domain", "MonatroxPortfolio.Projects.Public", false)]
    [InlineData("Profile", "Application", "MonatroxPortfolio.Projects.Infrastructure", false)]
    [InlineData("Profile", "Public", "MonatroxPortfolio.Profile.Domain", false)]
    [InlineData("Profile", "Infrastructure", "MonatroxPortfolio.Projects.Domain", false)]
    [InlineData("Profile", "Application", "MonatroxPortfolio.Projects.Public", true)]
    [InlineData("Profile", "Application", "MonatroxPortfolio.Profile.Domain", true)]
    public void Intentional_forbidden_reference_fixtures_are_rejected(string module, string layer, string target, bool allowed) =>
        Assert.Equal(allowed, Allowed(module, layer, target));

    [Fact]
    public void Domain_and_Public_are_framework_free()
    {
        foreach (var project in SourceFiles("Modules", "*.csproj").Where(p => p.EndsWith(".Domain.csproj") || p.EndsWith(".Public.csproj")))
        {
            var doc = XDocument.Load(project);
            Assert.Empty(doc.Descendants("PackageReference"));
            Assert.Empty(doc.Descendants("FrameworkReference"));
        }
    }

    [Fact]
    public void Entire_project_graph_is_acyclic()
    {
        var graph = SourceFiles(".", "*.csproj").ToDictionary(p => Path.GetFileNameWithoutExtension(p), References);
        foreach (var node in graph.Keys) Assert.False(Cycle(node, graph, []), $"Dependency cycle from {node}");
        Assert.True(Cycle("a", new Dictionary<string, string[]> { ["a"] = ["b"], ["b"] = ["a"] }, []));
    }
    private static bool Cycle(string node, Dictionary<string, string[]> graph, HashSet<string> path)
    {
        if (!path.Add(node)) return true;
        foreach (var child in graph[node]) if (Cycle(child, graph, path)) return true;
        path.Remove(node);
        return false;
    }

    [Fact]
    public void Module_source_does_not_name_foreign_implementations_or_bypass_boundaries()
    {
        foreach (var module in Modules)
        foreach (var file in SourceFiles($"Modules/{module}", "*.cs"))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(text, @"MonatroxPortfolio\.([A-Za-z]+)\.(Domain|Application|Infrastructure)\b"))
                Assert.True(match.Groups[1].Value == module, $"Foreign implementation: {file}: {match.Value}");
            Assert.DoesNotMatch(@"Assembly\.(Load|LoadFrom)|GetAssemblies\(", text);
            if (file.Contains(".Public/")) Assert.DoesNotMatch(@"DbContext|DbSet<|IQueryable<|Microsoft\.AspNetCore|Microsoft\.EntityFrameworkCore", text);
            foreach (var foreign in Modules.Where(x => x != module)) Assert.DoesNotContain($"\"{foreign}_", text);
        }
    }

    [Fact]
    public void Frontend_projects_have_no_backend_implementation_or_database_dependencies()
    {
        foreach (var host in new[] { "Blazor", "Blazor.Client" })
        {
            var project = Path.Combine(Root, $"Hosts/MonatroxPortfolio.{host}/MonatroxPortfolio.{host}.csproj");
            Assert.All(References(project), reference => Assert.True(host == "Blazor.Client"
                ? reference.EndsWith(".Public")
                : reference is "MonatroxPortfolio.Blazor.Client" or "MonatroxPortfolio.ServiceDefaults"));
            foreach (var file in SourceFiles($"Hosts/MonatroxPortfolio.{host}", "*"))
            {
                if (!file.EndsWith(".cs") && !file.EndsWith(".razor")) continue;
                Assert.DoesNotMatch(@"MonatroxPortfolio\.[A-Za-z]+\.(Domain|Application|Infrastructure)|EntityFrameworkCore|DbContext", File.ReadAllText(file));
            }
        }
        var client = XDocument.Load(Path.Combine(Root, "Hosts/MonatroxPortfolio.Blazor.Client/MonatroxPortfolio.Blazor.Client.csproj"));
        Assert.Empty(client.Descendants("FrameworkReference"));
        Assert.All(client.Descendants("PackageReference"), package =>
            Assert.Contains(package.Attribute("Include")!.Value, new[] { "Microsoft.AspNetCore.Components.WebAssembly", "MudBlazor", "Markdig" }));
    }

    [Fact]
    public void Frontend_uses_prerendered_WebAssembly_without_Server_or_Auto_fallback()
    {
        var program = File.ReadAllText(Path.Combine(Root, "Hosts/MonatroxPortfolio.Blazor/Program.cs"));
        Assert.Contains("AddInteractiveWebAssemblyComponents", program);
        Assert.Contains("AddInteractiveWebAssemblyRenderMode", program);
        Assert.DoesNotContain("AddInteractiveServer", program);
        var app = File.ReadAllText(Path.Combine(Root, "Hosts/MonatroxPortfolio.Blazor/Components/App.razor"));
        Assert.Contains("<Routes @rendermode=\"InteractiveWebAssembly\"", app);
        Assert.Contains("<HeadOutlet @rendermode=\"InteractiveWebAssembly\"", app);
        Assert.DoesNotContain("prerender: false", app);
        foreach (var file in SourceFiles("Hosts/MonatroxPortfolio.Blazor.Client", "*.razor"))
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("InteractiveServer", source);
            Assert.DoesNotContain("InteractiveAuto", source);
        }
    }

    [Fact]
    public void Endpoints_live_in_Application_and_runtime_hosts_do_not_migrate()
    {
        foreach (var file in SourceFiles("Modules", "*.cs").Concat(SourceFiles("Hosts/MonatroxPortfolio.Api", "*.cs")))
            if (Regex.IsMatch(File.ReadAllText(file), @"\.Map(Get|Post|Put|Patch|Delete)\(")) Assert.Contains(".Application/", file);
        foreach (var host in new[] { "Api", "Blazor" })
        {
            var source = File.ReadAllText(Path.Combine(Root, $"Hosts/MonatroxPortfolio.{host}/Program.cs"));
            Assert.DoesNotContain("EnsureCreated", source);
            Assert.DoesNotContain("MigrateAsync", source);
        }
    }

    [Fact]
    public void Shared_service_defaults_live_under_Shared()
    {
        var project = "Shared/MonatroxPortfolio.ServiceDefaults/MonatroxPortfolio.ServiceDefaults.csproj";
        Assert.True(File.Exists(Path.Combine(Root, project)));
        var solution = XDocument.Load(Path.Combine(Root, "MonatroxPortfolio.slnx"));
        var shared = Assert.Single(solution.Descendants("Folder"), f => (string?)f.Attribute("Name") == "/Shared/");
        Assert.Contains(shared.Elements("Project"), p => (string?)p.Attribute("Path") == project);
    }

    [Fact]
    public void Solution_folders_match_physical_ownership()
    {
        var solution = XDocument.Load(Path.Combine(Root, "MonatroxPortfolio.slnx"));
        foreach (var module in Modules)
        {
            var folder = Assert.Single(solution.Descendants("Folder"), f => (string?)f.Attribute("Name") == $"/Modules/{module}/");
            foreach (var layer in Layers) Assert.Contains(folder.Elements("Project"), p =>
                (string?)p.Attribute("Path") == $"Modules/{module}/MonatroxPortfolio.{module}.{layer}/MonatroxPortfolio.{module}.{layer}.csproj");
        }
        foreach (var project in solution.Descendants("Project"))
        {
            var path = project.Attribute("Path")!.Value;
            var expected = path.StartsWith("Modules/", StringComparison.Ordinal) ? $"/Modules/{path.Split('/')[1]}/" : $"/{path.Split('/')[0]}/";
            Assert.Equal(expected, project.Parent!.Attribute("Name")!.Value);
        }
        Assert.Equal(SourceFiles(".", "*.csproj").Count(), solution.Descendants("Project").Count());
    }
}
