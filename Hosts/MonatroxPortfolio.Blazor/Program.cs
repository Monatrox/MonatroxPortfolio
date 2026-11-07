using MonatroxPortfolio.Blazor.Components;
using MonatroxPortfolio.Blazor.Client.Components;
using MonatroxPortfolio.Blazor.Client.Services;
using MonatroxPortfolio.ServiceDefaults;
using MudBlazor.Services;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var apiAddress = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is required. Use Aspire or configure the API HTTP(S) address.");
if (!Uri.TryCreate(apiAddress, UriKind.Absolute, out var apiUri) || apiUri.Scheme is not ("http" or "https")
    || !string.IsNullOrEmpty(apiUri.UserInfo) || apiUri.AbsolutePath != "/" || !string.IsNullOrEmpty(apiUri.Query) || !string.IsNullOrEmpty(apiUri.Fragment))
    throw new InvalidOperationException("Api:BaseUrl must be an HTTP(S) origin without credentials, path, query or fragment.");
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient<PortfolioApiClient>(http =>
{
    http.BaseAddress = apiUri;
    http.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpForwarder();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseWebAssemblyDebugging();
else app.UseExceptionHandler("/error");
app.UseAntiforgery();
app.MapDefaultEndpoints();
app.MapForwarder("/api/{**path}", apiUri.AbsoluteUri, new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(15) });
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);
app.Run();

namespace MonatroxPortfolio.Blazor
{
    public sealed class BlazorAssemblyMarker;
}
