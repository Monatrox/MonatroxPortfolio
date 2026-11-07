using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MonatroxPortfolio.Blazor.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(15)
});
builder.Services.AddScoped<PortfolioApiClient>();
await builder.Build().RunAsync();
