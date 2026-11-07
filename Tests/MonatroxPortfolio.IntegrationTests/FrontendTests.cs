using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using MonatroxPortfolio.Blazor;

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
                    ? Results.Text(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/profile.json")), "application/json")
                    : Results.Problem(statusCode: (int)ProfileStatus, title: "upstream.profile_error");
            });
            app.MapGet("/api/projects", () =>
            {
                Interlocked.Increment(ref projectRequests);
                return Results.Text(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content/projects.json")), "application/json");
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
        Assert.Contains("Project 1", html);
        Assert.Contains("\"type\":\"webassembly\"", html);
        Assert.Contains("Blazor-WebAssembly-Component-State:", html);
        Assert.DoesNotContain("\"type\":\"server\"", html);
        Assert.DoesNotContain(upstream.Address, html);
        Assert.Equal(1, upstream.ProfileRequests);
        Assert.Equal(1, upstream.ProjectRequests);
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
