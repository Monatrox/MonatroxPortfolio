using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

if (args is ["--install"]) return Microsoft.Playwright.Program.Main(["install", "chromium"]);

if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var url) || !url.IsLoopback || url.Scheme is not ("http" or "https"))
{
    Console.Error.WriteLine("Usage: MonatroxPortfolio.BrowserTests <loopback-portfolio-url> | --install. Start and import local app first.");
    return 1;
}

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1920, Height = 1080 } });
var page = await context.NewPageAsync();
var errors = new List<string>();
page.PageError += (_, error) => errors.Add(error);
page.Console += (_, message) => { if (message.Type == "error") errors.Add(message.Text); };
page.SetDefaultTimeout(30000);
SetDefaultExpectTimeout(30000);
var browserApiRequests = new List<string>();
var circuitRequests = new List<string>();
page.Request += (_, request) =>
{
    var path = new Uri(request.Url).AbsolutePath;
    if (path.StartsWith("/api/", StringComparison.Ordinal)) browserApiRequests.Add(path);
    if (path.StartsWith("/_blazor", StringComparison.Ordinal)) circuitRequests.Add(request.Url);
};
page.WebSocket += (_, socket) =>
{
    if (new Uri(socket.Url).AbsolutePath.StartsWith("/_blazor", StringComparison.Ordinal)) circuitRequests.Add(socket.Url);
};
await page.GotoAsync(url.ToString());
await Expect(page.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByAltText("MX logo")).ToBeVisibleAsync();
await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(24, 24, 24)");
var skills = page.GetByRole(AriaRole.Button, new() { Name = "Tools, Skills, and Languages" });
await Expect(page.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
await Expect(skills).ToBeEnabledAsync();
if (browserApiRequests.Count != 0) throw new InvalidOperationException("Prerender handoff refetched API data: " + string.Join(", ", browserApiRequests));
await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Monatrox", Exact = true })).Not.ToBeFocusedAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
await Expect(page.GetByText("Tools/Frameworks", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByText("Rider", new() { Exact = true })).ToBeVisibleAsync();
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
var background = page.GetByRole(AriaRole.Button, new() { Name = "Background and Education" });
await background.ClickAsync();
await Expect(page.GetByText("Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByText("Bachelor of Science (B.S.), Computer Science", new() { Exact = true })).ToBeVisibleAsync();
await background.ClickAsync();

var carousel = page.GetByTestId("project-carousel");
await carousel.GetByRole(AriaRole.Button, new() { Name = "Index 1", Exact = true }).ClickAsync();
var project = page.GetByRole(AriaRole.Button, new() { Name = "Project 1 Description 1", Exact = true });
await project.FocusAsync();
await page.Keyboard.PressAsync("Enter");
await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
await Expect(page.GetByText("Project Image Placeholder", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByText("Additional Information", new() { Exact = true })).ToBeVisibleAsync();
await page.Keyboard.PressAsync("Escape");
await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
await carousel.GetByRole(AriaRole.Button, new() { Name = "Index 1", Exact = true }).ClickAsync();
await carousel.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
await Expect(page.GetByText("Project 4", new() { Exact = true })).ToBeVisibleAsync();
await context.SetOfflineAsync(true);
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
await context.SetOfflineAsync(false);

await using var noScript = await browser.NewContextAsync(new() { JavaScriptEnabled = false });
var staticPage = await noScript.NewPageAsync();
await staticPage.GotoAsync(url.ToString());
await Expect(staticPage.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Tools, Skills, and Languages" })).ToBeDisabledAsync();
foreach (var status in new[] { 404, 503 })
{
    await using var failureContext = await browser.NewContextAsync();
    await failureContext.RouteAsync("**/api/profile", route => route.FulfillAsync(new()
    {
        Status = status, ContentType = "application/problem+json", Body = "{\"title\":\"simulated API failure\"}"
    }));
    var failurePage = await failureContext.NewPageAsync();
    failurePage.PageError += (_, error) => errors.Add(error);
    var requests = new List<Uri>();
    failurePage.Request += (_, request) =>
    {
        var address = new Uri(request.Url);
        if (address.AbsolutePath.StartsWith("/api/", StringComparison.Ordinal)) requests.Add(address);
    };
    await failurePage.GotoAsync(new Uri(url, "error").ToString());
    await Expect(failurePage.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
    await failurePage.GetByRole(AriaRole.Link, new() { Name = "Back to portfolio" }).ClickAsync();
    var message = status == 404 ? "Portfolio content has not been imported yet." : "Portfolio data is unavailable. Please retry.";
    await Expect(failurePage.GetByRole(AriaRole.Alert)).ToContainTextAsync(message);
    await failureContext.UnrouteAsync("**/api/profile");
    await failurePage.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
    await Expect(failurePage.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
    await Expect(failurePage.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    if (requests.Any(address => address.GetLeftPart(UriPartial.Authority) != url.GetLeftPart(UriPartial.Authority)))
        throw new InvalidOperationException("Browser API request escaped frontend origin.");
    if (requests.Count(address => address.AbsolutePath == "/api/profile") != 2)
        throw new InvalidOperationException("Expected one failed profile request and one successful retry.");
}
if (circuitRequests.Count != 0) throw new InvalidOperationException("Unexpected Blazor Server circuit: " + string.Join(", ", circuitRequests));
if (errors.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
Console.WriteLine("WASM smoke passed: prerendered HTML without JS, WebAssembly runtime, no handoff refetch/circuits/autofocus, offline local interactions, dialogs/carousel, same-origin 404/503 recovery.");
return 0;
