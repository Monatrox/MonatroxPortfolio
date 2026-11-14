using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using MonatroxPortfolio.Blazor;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Profile.Public;
using MonatroxPortfolio.Projects.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.IntegrationTests;

public sealed class FrontendTests
{
    private sealed class FrontendFactory(string apiAddress) : WebApplicationFactory<BlazorAssemblyMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Test")
            .UseSetting("Api:BaseUrl", apiAddress);
    }

    private sealed class UpstreamApi : IAsyncDisposable
    {
        private readonly WebApplication app;
        private int profileRequests;
        private int projectRequests;
        private int pageRequests;
        private int contactRequests;
        public HttpStatusCode ContactStatus { get; set; } = HttpStatusCode.OK;
        public int ContactRequests => Volatile.Read(ref contactRequests);
        public bool UnsafeMarkdown { get; set; }
        public string? ProfileJson { get; set; }
        public int PageRequests => Volatile.Read(ref pageRequests);
        public HttpStatusCode ProfileStatus { get; set; } = HttpStatusCode.OK;
        public int ProfileRequests => Volatile.Read(ref profileRequests);
        public int ProjectRequests => Volatile.Read(ref projectRequests);
        public string Address => app.Urls.Single();

        public UpstreamApi()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            app = builder.Build();
            app.MapGet("/api/profile", () =>
            {
                Interlocked.Increment(ref profileRequests);
                return ProfileStatus == HttpStatusCode.OK
                    ? Results.Text(ProfileJson ?? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/profile.json")), "application/json")
                    : Results.Problem(statusCode: (int)ProfileStatus, title: "upstream.profile_error");
            });
            app.MapGet("/api/projects", () =>
            {
                Interlocked.Increment(ref projectRequests);
                var projects = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "content/project-pages"), "*.json")
                    .Select(file => JsonSerializer.Deserialize<ProjectImportDto>(File.ReadAllText(file), ContentJson.Options)!)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new ProjectDto(x.Id, x.Name, x.Description, x.ThumbnailSrc, x.Link, x.SortOrder));
                return Results.Json(projects);
            });
            app.MapGet("/api/projects/{id}/page", (string id) =>
            {
                Interlocked.Increment(ref pageRequests);
                if (id is not ("ccf" or "distill" or "portfolio")) return Results.NotFound();
                var input = JsonSerializer.Deserialize<ProjectImportDto>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/project-pages", id + ".json")), ContentJson.Options)!;
                var document = UnsafeMarkdown ? input.Page! with { Notes = ["<script id='unsafe'>alert(1)</script>", "[bad](javascript:alert%281%29)"] } : input.Page!;
                return Results.Json(new ProjectPageDto(id, document));
            });
            app.MapGet("/api/contact", () =>
            {
                Interlocked.Increment(ref contactRequests);
                return ContactStatus == HttpStatusCode.OK ? Results.Json(new ContactDeliveryInfoDto(true, true))
                    : Results.Problem(statusCode: (int)ContactStatus);
            });
            app.MapGet("/api/echo", (HttpRequest request) => Results.Json(new { Path = request.Path.Value, Query = request.QueryString.Value }));
            app.MapGet("/api/refused", () => Results.Problem(statusCode: 409, title: "upstream.conflict", detail: "Preserve this response."));
            app.MapGet("/api/redirect", () => Results.Redirect("/api/profile"));
        }

        public Task StartAsync() => app.StartAsync();
        public Task StopAsync() => app.StopAsync();
        public async ValueTask DisposeAsync() => await app.DisposeAsync();
    }

    [Fact]
    public async Task Initial_html_prerenders_public_content_once_and_contains_WASM_handoff_state()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("Team Lead at Falkon Technologies", html);
        AssertMetadata(html, "Zac Taylor — Engineering & Technical Leadership", "Zac Taylor's portfolio: backend engineering, technical leadership, and independently built software. Explore projects, experience, and ways to get in contact.", "/");
        Assert.Contains("CCF", html);
        Assert.Contains("Distill", html);
        Assert.Contains("Read Portfolio case study", html);
        Assert.Contains("href=\"/Content/Resume/zac-taylor-resume.pdf\"", html);
        Assert.Contains("download=\"zac-taylor-resume.pdf\"", html);
        Assert.Contains("data-enhance-nav=\"false\"", html);
        Assert.Contains("Download résumé", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("Resume/Previews", html);
        Assert.Contains("id=\"skill-title-Web\"", html);
        Assert.Contains("id=\"skill-title-Architecture\"", html);
        Assert.Contains("id=\"skill-title-AI\"", html);
        Assert.Contains("class=\"skill-list\"", html);
        Assert.Contains("Rider, DataGrip, and other JetBrains tools", html);
        Assert.Contains("Postman, Insomnia, and Bruno", html);
        Assert.Contains("Unreal Engine and Unity", html);
        Assert.Contains("Engineering standards &amp; technical reviews", html);
        Assert.Contains("Mocking, stubs &amp; test doubles", html);
        Assert.Contains("In-process application &amp; API integration tests", html);
        Assert.Contains("Testcontainers", html);
        Assert.Contains("Architecture fitness", html);
        Assert.Contains("A/B testing", html);
        Assert.Contains("Canary testing", html);
        Assert.Contains("Feature flags &amp; dark deployments", html);
        Assert.DoesNotContain("Code review &amp; pull requests", html);
        Assert.Contains("A selection of technologies and practices from my experience, not an exhaustive inventory.", html);
        Assert.Contains("IIS server administration", html);
        Assert.Contains("Azure deployments", html);
        Assert.Contains("Custom AI skill files", html);
        Assert.Contains("MCP integrations", html);
        Assert.Contains("Valkey / Redis", html);
        Assert.Contains("legacy maintenance", html);
        Assert.DoesNotContain("skill-title-DotNet", html);
        Assert.DoesNotContain("Gamer layout previews", html);
        Assert.DoesNotContain("data-gamer-preview=", html);
        Assert.Contains("gamer-theme.", html);
        Assert.DoesNotContain("gamer-previews.", html);
        Assert.DoesNotContain("neon-lab", html);
        Assert.DoesNotContain("Open Project 1 details", html);
        Assert.Contains("\"type\":\"webassembly\"", html);
        Assert.Contains("Blazor-WebAssembly-Component-State:", html);
        Assert.DoesNotContain("\"type\":\"server\"", html);
        Assert.DoesNotContain(upstream.Address, html);
        Assert.Equal(1, upstream.ProfileRequests);
        Assert.Equal(1, upstream.ProjectRequests);
    }

    private static void AssertMetadata(string html, string title, string description, string path)
    {
        var decoded = WebUtility.HtmlDecode(html);
        Assert.Contains($"<title>{title}</title>", decoded);
        Assert.Contains($"name=\"description\" content=\"{description}\"", decoded);
        Assert.Contains($"rel=\"canonical\" href=\"https://monatrox.com{path}\"", decoded);
        Assert.Contains($"property=\"og:title\" content=\"{title}\"", decoded);
        Assert.Contains($"property=\"og:description\" content=\"{description}\"", decoded);
        Assert.Contains($"property=\"og:url\" content=\"https://monatrox.com{path}\"", decoded);
        Assert.Contains("property=\"og:image\" content=\"https://monatrox.com/Content/Images/portfolio-share.png\"", decoded);
        Assert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", decoded);
        Assert.Single(Regex.Matches(html, "name=\"description\""));
        Assert.Single(Regex.Matches(html, "rel=\"canonical\""));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Inline_contact_prerenders_status_once_with_disabled_controls_and_retained_failure(HttpStatusCode status)
    {
        await using var upstream = new UpstreamApi { ContactStatus = status };
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var contact = html[html.IndexOf("id=\"contact\"", StringComparison.Ordinal)..];
        Assert.Contains("Get in Contact", contact);
        Assert.Contains("Your name", contact);
        Assert.Contains("Your email", contact);
        Assert.Contains("Message", contact);
        Assert.Contains("contact-inline", contact);
        Assert.Contains("No message database is kept by this site.", contact);
        Assert.Matches("<input[^>]*disabled", contact);
        Assert.Matches("<textarea[^>]*disabled", contact);
        Assert.Matches("<button[^>]*disabled[^>]*>.*?Send message", contact);
        Assert.DoesNotContain("support-toggle", contact);
        if (status == HttpStatusCode.OK) Assert.Contains("Local preview: messages are captured", contact);
        else Assert.Contains("Email service is unavailable. Please try again later or use LinkedIn.", contact);
        Assert.Equal(1, upstream.ContactRequests);
    }

    [Fact]
    public async Task Resume_is_a_public_pdf_without_exposing_archived_originals_or_comparisons()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Content/Resume/zac-taylor-resume.pdf");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var pdf = await response.Content.ReadAsByteArrayAsync();
        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.True(pdf.Length > 10_000);
        foreach (var path in new[] { "/Content/Resume/Previews/index.html", "/Content/Resume/Previews/zac-taylor-resume-portfolio.pdf", "/content/resume/originals/zac-taylor-resume-portfolio.pdf" })
        {
            using var archived = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, archived.StatusCode);
        }
        Assert.Equal(0, upstream.ProfileRequests);
        Assert.Equal(0, upstream.ProjectRequests);
    }

    [Fact]
    public async Task Share_preview_is_a_local_1200_by_630_png()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Content/Images/portfolio-share.png");
        response.EnsureSuccessStatusCode();
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        var image = await response.Content.ReadAsByteArrayAsync();
        Assert.True(image.Length >= 24);
        Assert.Equal(1200, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(16, 4)));
        Assert.Equal(630, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(20, 4)));
        Assert.Equal(0, upstream.ProjectRequests);
    }

    [Fact]
    public async Task Influences_prerenders_ordered_books_without_fetching_artwork_or_extra_API_reads()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var influences = html[html.IndexOf("id=\"influences\"", StringComparison.Ordinal)..];
        Assert.Contains("aria-label=\"Influences\"", influences);
        Assert.Contains("aria-controls=\"influences-content\"", influences);
        Assert.Matches("<p[^>]*id=\"influences-description\"[^>]*>Books that shape the way I think about my work</p>", influences);
        Assert.Contains("aria-describedby=\"influences-description\"", influences);
        Assert.Contains("aria-label=\"Influences and profile view\"", influences);
        var switches = Regex.Matches(influences, "<button[^>]*aria-label=\"Show (?:books and professional|games and gamer) profile\"[^>]*>");
        Assert.Equal(2, switches.Count);
        foreach (Match button in switches) Assert.Contains("disabled", button.Value);
        Assert.Contains("aria-pressed=\"true\"", switches[0].Value);
        Assert.Contains("aria-pressed=\"false\"", switches[1].Value);
        Assert.DoesNotContain(">Books</h3>", influences);
        Assert.Equal(8, Regex.Matches(influences, "class=\"influence-item\"").Count);
        Assert.DoesNotContain("src=\"Content/Images/Influences/", influences);
        Assert.DoesNotContain("-atlas.webp", influences);
        var previous = -1;
        foreach (var title in new[] { "Accelerate", "The Phoenix Project", "The Unicorn Project", "Team Topologies", "Atomic Habits", "Extreme Ownership", "The DevOps Handbook", "No Rules Rules" })
        {
            var index = influences.IndexOf($">{title}</figcaption>", StringComparison.Ordinal);
            Assert.True(index > previous, $"Missing or out-of-order book: {title}");
            previous = index;
        }
        Assert.DoesNotContain("itrevolution.com", influences);
        Assert.DoesNotContain("penguinrandomhouse.com", influences);
        Assert.DoesNotContain("macmillan.com", influences);
        Assert.Equal(1, upstream.ProfileRequests);
        Assert.Equal(1, upstream.ProjectRequests);
    }

    [Fact]
    public async Task Tool_presentation_keeps_partial_groups_and_unlisted_tools_visible()
    {
        var profile = JsonSerializer.Deserialize<ProfileDto>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/profile.json")), ContentJson.Options)!;
        profile = profile with { Skills = [
            new("insomnia", "Insomnia", "Workflow", 1),
            new("unity", "Unity", "Workflow", 2),
            new("datagrip", "DataGrip", "Workflow", 3),
            new("custom-tool", "Preserved custom tool", "Workflow", 4)
        ] };
        await using var upstream = new UpstreamApi { ProfileJson = JsonSerializer.Serialize(profile, ContentJson.Options) };
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains(">Insomnia</li>", html);
        Assert.Contains(">Unity</li>", html);
        Assert.Contains("DataGrip and other JetBrains tools", html);
        Assert.Contains("Preserved custom tool", html);
        Assert.DoesNotContain("Postman", html);
        Assert.DoesNotContain("Unreal Engine", html);
        Assert.DoesNotContain("Rider", html);
    }

    [Fact]
    public async Task Social_controls_are_paired_without_hiding_additional_profile_links()
    {
        var profile = JsonSerializer.Deserialize<ProfileDto>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/profile.json")), ContentJson.Options)!;
        profile = profile with { SocialLinks = [.. profile.SocialLinks, new("extra", "Additional profile", "https://example.com/profile", 4)] };
        await using var upstream = new UpstreamApi { ProfileJson = JsonSerializer.Serialize(profile, ContentJson.Options) };
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var pairs = Regex.Matches(html, "<div class=\"social-pair\">(.*?)</div>", RegexOptions.Singleline);
        Assert.Equal(3, pairs.Count);
        Assert.Equal(new[] { 2, 2, 1 }, pairs.Select(pair => Regex.Matches(pair.Value, "<(?:a|button)\\b").Count));
        Assert.Contains("https://example.com/profile", html);
        Assert.Contains("Copy Discord username monatrox", html);
        Assert.Contains("Email — Contact Me", WebUtility.HtmlDecode(html));
    }

    [Theory]
    [InlineData("ccf", "CCF")]
    [InlineData("distill", "Distill")]
    [InlineData("portfolio", "Portfolio")]
    public async Task Project_pages_prerender_full_body_and_handoff_once(string id, string title)
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/projects/" + id + "?utm_source=check");
        Assert.Contains(">" + title + "</h1>", html);
        var document = JsonSerializer.Deserialize<ProjectImportDto>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/project-pages", id + ".json")), ContentJson.Options)!;
        AssertMetadata(html, title + " — Zac Taylor", document.Page!.Hero.Subtitle, "/projects/" + id);
        Assert.Contains("Blazor-WebAssembly-Component-State:", html);
        Assert.DoesNotContain("\"type\":\"server\"", html);
        Assert.Equal(1, upstream.PageRequests);
        Assert.Equal(0, upstream.ProfileRequests);
        Assert.Equal(0, upstream.ProjectRequests);
        if (id == "ccf") Assert.Contains("No production records are loaded.", html);
        else if (id == "distill")
        {
            Assert.Contains("99.90%", html);
            Assert.Contains("&lt;DistillClient&gt;", html);
            Assert.Contains("The <code>ILogger</code> provider batches", html);
            Assert.DoesNotContain("github.com/Monatrox/distill/blob/", html);
            Assert.Contains("Optional startup sink manifest", html);
        }
        else
        {
            Assert.Contains("Three modules. One API.", html);
            Assert.Contains("Intentionally overengineered.", html);
            Assert.Contains("demonstration of Aspire and a modular monolith", html);
            Assert.Contains("Aspire: run it, then inspect it", html);
            Assert.Contains("Where it&#x27;s overengineered", html);
            Assert.Contains("GitHub Actions", html);
            Assert.Contains("Google Workspace", html);
            Assert.Contains("applies migrations and imports content", html);
            Assert.DoesNotContain("BoringPortfolio", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Missing_project_has_no_share_metadata_and_is_not_indexed()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/projects/missing");
        Assert.Contains("name=\"robots\" content=\"noindex\"", html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
        Assert.DoesNotContain("property=\"og:url\"", html);
    }

    [Fact]
    public async Task Markdown_does_not_execute_html_or_unsafe_links_even_when_upstream_bypasses_import_validation()
    {
        await using var upstream = new UpstreamApi { UnsafeMarkdown = true };
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/projects/ccf");
        Assert.Contains("&lt;script", html);
        Assert.DoesNotContain("<script id=", html);
        Assert.DoesNotContain("href=\"javascript:", html);
    }

    [Fact]
    public async Task Gateway_preserves_path_query_and_error_response_without_request_selected_destination()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        using var echo = JsonDocument.Parse(await client.GetStringAsync("/api/echo?destination=http%3A%2F%2Fexample.invalid&value=one"));
        Assert.Equal("/api/echo", echo.RootElement.GetProperty("path").GetString());
        Assert.Contains("destination=http%3A%2F%2Fexample.invalid", echo.RootElement.GetProperty("query").GetString());
        var error = await client.GetAsync("/api/refused");
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("application/problem+json", error.Content.Headers.ContentType!.MediaType);
        Assert.Contains("upstream.conflict", await error.Content.ReadAsStringAsync());
        var redirect = await client.GetAsync("/api/redirect");
        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        Assert.Equal("/api/profile", redirect.Headers.Location!.OriginalString);
        Assert.Equal(0, upstream.ProfileRequests);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/missing")).StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Portfolio content has not been imported yet.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Portfolio data is unavailable. Please retry.")]
    public async Task Prerendered_errors_include_retry_and_persist_for_client_handoff(HttpStatusCode status, string message)
    {
        await using var upstream = new UpstreamApi { ProfileStatus = status };
        await upstream.StartAsync();
        await using var factory = new FrontendFactory(upstream.Address);
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains(message, html);
        Assert.Contains("Retry", html);
        Assert.Contains("Blazor-WebAssembly-Component-State:", html);
        Assert.Equal(1, upstream.ProfileRequests);
        Assert.Equal(1, upstream.ProjectRequests);
    }

    [Fact]
    public async Task Unavailable_backend_returns_gateway_error_but_frontend_remains_healthy()
    {
        await using var upstream = new UpstreamApi();
        await upstream.StartAsync();
        var address = upstream.Address;
        await upstream.StopAsync();
        await using var factory = new FrontendFactory(address);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync("/api/profile")).StatusCode);
        Assert.Contains("Portfolio data is unavailable. Please retry.", await client.GetStringAsync("/"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Theory]
    [InlineData("ftp://127.0.0.1")]
    [InlineData("http://user:password@127.0.0.1")]
    [InlineData("http://127.0.0.1/base")]
    [InlineData("http://127.0.0.1?target=other")]
    [InlineData("http://127.0.0.1#fragment")]
    public async Task Gateway_rejects_invalid_upstream_configuration(string apiAddress)
    {
        await using var factory = new FrontendFactory(apiAddress);
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Api:BaseUrl must be an HTTP(S) origin", error.Message);
    }
}
