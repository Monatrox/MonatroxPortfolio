using MonatroxPortfolio.BlazorServer.Components;
using MonatroxPortfolio.BlazorServer.Services;
using MonatroxPortfolio.ServiceDefaults;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient<PortfolioApiClient>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://api");
    http.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error");
app.UseStaticFiles();
app.UseAntiforgery();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
