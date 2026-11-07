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

var app = builder.Build();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapProfileEndpoints();
app.MapProjectEndpoints();
app.Run();

namespace MonatroxPortfolio.Api
{
    public sealed class ApiAssemblyMarker;
}
