using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME",
    Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME") ?? "podman");
var builder = DistributedApplication.CreateBuilder(args);
var root = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../.."));
var data = Path.Combine(root, "data");
Directory.CreateDirectory(data);
var connection = builder.Configuration.GetConnectionString("Portfolio")
    ?? $"Data Source={Path.Combine(data, "portfolio.db")};Foreign Keys=True";

var smtp = builder.AddContainer("smtp4dev", "rnwood/smtp4dev", "3.16.1")
    .WithImageRegistry("docker.io")
    .WithEnvironment("ServerOptions__Urls", "http://0.0.0.0:80")
    .WithEndpoint(targetPort: 25, name: "smtp")
    .WithHttpEndpoint(port: 5430, targetPort: 80, name: "http")
    .WithHttpHealthCheck("/", endpointName: "http");

var migrations = builder.AddProject<Projects.MonatroxPortfolio_Migrations>("migrations")
    .WithEnvironment("ConnectionStrings__Portfolio", connection);
var api = builder.AddProject<Projects.MonatroxPortfolio_Api>("api", "http")
    .WithEnvironment("ConnectionStrings__Portfolio", connection)
    .WithEnvironment("Smtp__Host", smtp.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Smtp__Port", smtp.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");
builder.AddProject<Projects.MonatroxPortfolio_BlazorServer>("blazor", "http")
    .WithReference(api)
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
    .WaitFor(api)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
