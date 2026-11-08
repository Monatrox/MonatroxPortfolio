using Microsoft.AspNetCore.HttpOverrides;
using MonatroxPortfolio.Contact.Application;
using MonatroxPortfolio.Contact.Infrastructure;
using MonatroxPortfolio.Profile.Application.Features.Read;
using MonatroxPortfolio.Profile.Infrastructure;
using MonatroxPortfolio.Projects.Application.Features.Read;
using MonatroxPortfolio.Projects.Infrastructure;
using MonatroxPortfolio.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
var connection = builder.Configuration.GetConnectionString("Portfolio")
    ?? throw new InvalidOperationException("ConnectionStrings:Portfolio is required. Use Aspire or scripts/dev.sh.");
builder.Services.AddProfile(connection);
builder.Services.AddProjects(connection);
builder.Services.AddContact(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
// shortcut: gateways are loopback-trusted only, configure known proxies before off-host deployment.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor, ForwardLimit = 1 });
app.UseRateLimiter();
app.MapDefaultEndpoints();
app.MapProfileEndpoints();
app.MapProjectEndpoints();
app.MapContactEndpoints();
app.Run();

namespace MonatroxPortfolio.Api
{
    public sealed class ApiAssemblyMarker;
}
