using System.Text.RegularExpressions;
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
var screenshots = Path.Combine(Path.GetTempPath(), "monatrox-professional-design");
Directory.CreateDirectory(screenshots);
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
var wasmStartup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
await context.RouteAsync("**/_framework/**", async route => { await wasmStartup.Task; await route.ContinueAsync(); });
try
{
    await page.GotoAsync(url.ToString(), new() { WaitUntil = WaitUntilState.Commit });
    await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show Zac Taylor", Exact = true })).ToBeDisabledAsync();
    await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Show Monatrox", Exact = true })).ToBeDisabledAsync();
    await CheckSelectorColorsAsync(page);
    await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "prerender.png"), Animations = ScreenshotAnimations.Disabled });
}
finally
{
    wasmStartup.TrySetResult();
}
await context.UnrouteAsync("**/_framework/**");
await Expect(page.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByAltText("MX logo")).ToBeVisibleAsync();
await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(245, 243, 236)");
var favicon = page.Locator("head link[rel='icon']");
await Expect(favicon).ToHaveAttributeAsync("type", "image/png");
await Expect(favicon).ToHaveAttributeAsync("sizes", "64x64");
var faviconHref = await favicon.GetAttributeAsync("href") ?? throw new InvalidOperationException("Missing favicon.");
if (!Regex.IsMatch(faviconHref, @"^favicon\.[a-z0-9]+\.png$")) throw new InvalidOperationException("Favicon must use a fingerprinted URL.");
using (var http = new HttpClient())
{
    using var response = await http.GetAsync(new Uri(url, faviconHref));
    response.EnsureSuccessStatusCode();
    var png = await response.Content.ReadAsByteArrayAsync();
    if (response.Content.Headers.ContentType?.MediaType != "image/png" || png.Length < 24 ||
        System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)) != 64 ||
        System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)) != 64)
        throw new InvalidOperationException("Favicon must serve the 64px logo PNG.");
}
await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Explore projects" })).ToHaveCSSAsync("background-color", "rgb(168, 212, 56)");
var skills = page.GetByRole(AriaRole.Button, new() { Name = "Tools, Skills, and Languages" });
await Expect(page.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
await Expect(skills).ToBeEnabledAsync();
if (browserApiRequests.Count != 0) throw new InvalidOperationException("Prerender handoff refetched API data: " + string.Join(", ", browserApiRequests));
await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Zac Taylor", Exact = true })).Not.ToBeFocusedAsync();
var professionalView = page.GetByRole(AriaRole.Button, new() { Name = "Show Zac Taylor", Exact = true });
var gamerView = page.GetByRole(AriaRole.Button, new() { Name = "Show Monatrox", Exact = true });
await CheckPersonalLabelsAsync();
await CheckSelectorColorsAsync(page);
await Expect(page.Locator(".identity-visual figcaption")).ToHaveTextAsync("Zac Taylor");
await Expect(professionalView).ToBeVisibleAsync();
await Expect(gamerView).ToBeVisibleAsync();
await Expect(professionalView).ToHaveAttributeAsync("aria-pressed", "true");
await Expect(gamerView).ToHaveAttributeAsync("aria-pressed", "false");
await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Choose identity" })).ToHaveCSSAsync("position", "static");
if (!await professionalView.Locator(".identity-name").EvaluateAsync<bool>("element => element.firstElementChild?.tagName.toLowerCase() === 'svg'"))
    throw new InvalidOperationException("Professional icon must precede name.");
if (!await gamerView.Locator(".identity-name").EvaluateAsync<bool>("element => element.lastElementChild?.tagName.toLowerCase() === 'svg'"))
    throw new InvalidOperationException("Gamer icon must follow name.");
var portrait = page.GetByAltText("Portrait of Zac Taylor", new() { Exact = true });
await Expect(portrait).ToBeVisibleAsync();
await Expect(portrait).ToHaveAttributeAsync("src", "Content/Images/zac-taylor-portrait.jpg");
await Expect(portrait).ToHaveJSPropertyAsync("complete", true);
if (!await portrait.EvaluateAsync<bool>("image => image.naturalWidth > 0 && image.getBoundingClientRect().height >= 240"))
    throw new InvalidOperationException("Prominent portrait missing or too small.");
if (!await portrait.EvaluateAsync<bool>("image => Math.abs(image.getBoundingClientRect().height - image.parentElement.getBoundingClientRect().height) < 1"))
    throw new InvalidOperationException("Portrait must fill reserved image frame.");
var gamerBounds = await gamerView.BoundingBoxAsync();
var professionalBounds = await professionalView.BoundingBoxAsync();
if (gamerBounds is null || professionalBounds is null || gamerBounds.Y + gamerBounds.Height > professionalBounds.Y)
    throw new InvalidOperationException("Identity names must be stacked.");
if (!await professionalView.Locator(".identity-name").EvaluateAsync<bool>("element => parseFloat(getComputedStyle(element).fontSize) >= 80"))
    throw new InvalidOperationException("Desktop identity selector must dominate hero typography.");
foreach (var (width, height) in new[] { (1920, 1080), (1366, 768) })
{
    await page.SetViewportSizeAsync(width, height);
    if (!await page.EvaluateAsync<bool>("""
        () => {
            const about = document.querySelector('#about').getBoundingClientRect();
            const projects = document.querySelector('#projects').getBoundingClientRect();
            const hero = document.querySelector('.identity-stage').getBoundingClientRect();
            const main = ['.hero-identity', '.identity-visual', '.hero-contact'].map(selector => document.querySelector(selector).getBoundingClientRect());
            return about.top >= window.innerHeight - 1 && projects.top > about.top &&
                hero.height >= window.innerHeight - hero.top - 1 && main.every(box => box.top >= 0 && box.bottom <= window.innerHeight);
        }
        """))
        throw new InvalidOperationException($"Identity-first screen or below-fold details broken at {width}x{height}.");
}
await page.SetViewportSizeAsync(1920, 1080);
await CheckAboutPortraitAsync();
await page.Locator("#about").ScreenshotAsync(new() { Path = Path.Combine(screenshots, "about-professional.png"), Animations = ScreenshotAnimations.Disabled });
await skills.ClickAsync();
await gamerView.FocusAsync();
await page.Keyboard.PressAsync("Enter");
await Expect(page.Locator(".portfolio-shell")).ToHaveAttributeAsync("data-view", "gamer");
await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(24, 26, 24)");
await CheckPersonalLabelsAsync();
await CheckSelectorColorsAsync(page, gamer: true);
await Expect(page.Locator(".identity-visual figcaption")).ToHaveTextAsync("Monatrox");
await Expect(page).ToHaveTitleAsync("Monatrox");
await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Monatrox", Exact = true })).ToBeVisibleAsync();
await Expect(professionalView).ToBeVisibleAsync();
await Expect(gamerView).ToBeVisibleAsync();
await Expect(gamerView).ToHaveAttributeAsync("aria-pressed", "true");
await Expect(professionalView).ToHaveAttributeAsync("aria-pressed", "false");
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
await Expect(page.GetByAltText("Monatrox identity logo", new() { Exact = true })).ToHaveAttributeAsync("src", "Content/Images/MX Abstract Logo Cleaned.png");
await Expect(portrait).ToHaveCountAsync(0);
await CheckAboutPortraitAsync(gamer: true);
await page.Locator("#about").ScreenshotAsync(new() { Path = Path.Combine(screenshots, "about-gamer.png"), Animations = ScreenshotAnimations.Disabled });
await CheckDetailAlignmentAsync("skills-content");
await skills.ClickAsync();
await Expect(page.Locator("#skills-content")).ToHaveCSSAsync("height", "0px");
await page.Locator(".hero-statement").ClickAsync();
await page.Mouse.MoveAsync(0, 0);
await page.EvaluateAsync("window.scrollTo({ top: 0, behavior: 'instant' })");
await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "gamer-desktop.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
await professionalView.ClickAsync();
await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(245, 243, 236)");
await Expect(portrait).ToBeVisibleAsync();
if (browserApiRequests.Count != 0) throw new InvalidOperationException("View switching refetched profile/project data.");
await page.ReloadAsync();
await Expect(page.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
await page.Keyboard.PressAsync("Tab");
await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Skip to content" })).ToBeFocusedAsync();
await page.Keyboard.PressAsync("Enter");
await Expect(page.Locator("#main-content")).ToBeFocusedAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
await Expect(page.GetByText("Tools/Frameworks", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByText("Rider", new() { Exact = true })).ToBeVisibleAsync();
foreach (var width in new[] { 1920, 1366, 1024 })
{
    await page.SetViewportSizeAsync(width, 1080);
    await CheckDetailAlignmentAsync("skills-content");
    await CheckAboutPortraitAsync();
}
await page.SetViewportSizeAsync(1920, 1080);
await page.Locator("#skills-content").ScreenshotAsync(new() { Path = Path.Combine(screenshots, "skills-aligned.png"), Animations = ScreenshotAnimations.Disabled });
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
var background = page.GetByRole(AriaRole.Button, new() { Name = "Background and Education" });
await background.ClickAsync();
await Expect(page.GetByText("Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByText("Bachelor of Science (B.S.), Computer Science", new() { Exact = true })).ToBeVisibleAsync();
foreach (var width in new[] { 1920, 1366, 1024 })
{
    await page.SetViewportSizeAsync(width, 1080);
    await CheckDetailAlignmentAsync("background-content");
}
await page.SetViewportSizeAsync(1920, 1080);
await page.Locator("#background-content").ScreenshotAsync(new() { Path = Path.Combine(screenshots, "background-aligned.png"), Animations = ScreenshotAnimations.Disabled });
await background.ClickAsync();

var projects = page.GetByTestId("project-grid");
await Expect(projects.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
await Expect(projects.GetByRole(AriaRole.Link)).ToHaveCountAsync(3);
await Expect(projects.Locator(".project-reference")).ToHaveTextAsync(["ccf", "distill", "portfolio"]);
var project = projects.GetByRole(AriaRole.Link, new() { Name = "Read CCF case study", Exact = true });
await project.FocusAsync();
await page.Keyboard.PressAsync("Enter");
await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
await Expect(page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Heading, new() { Name = "CCF", Exact = true, Level = 1 })).ToBeVisibleAsync();
await Expect(page.GetByText("Project Image Placeholder", new() { Exact = true })).ToHaveCountAsync(0);
await page.Keyboard.PressAsync("Escape");
await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
await Expect(project).ToBeFocusedAsync();
await Expect(projects.GetByRole(AriaRole.Link, new() { Name = "Read Distill case study", Exact = true })).ToBeVisibleAsync();
await context.SetOfflineAsync(true);
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
await skills.ClickAsync();
await Expect(skills).ToHaveAttributeAsync("aria-expanded", "false");
await context.SetOfflineAsync(false);
await page.GetByRole(AriaRole.Link, new() { Name = "Back to top" }).ClickAsync();
await Expect(page.Locator("#main-content")).ToBeFocusedAsync();
await page.Locator(".hero-statement").ClickAsync();
await page.Mouse.MoveAsync(0, 0);
await page.EvaluateAsync("window.scrollTo({ top: 0, behavior: 'instant' })");
await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "desktop.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
foreach (var width in new[] { 320, 390, 768 })
{
    await page.SetViewportSizeAsync(width, 844);
    await Expect(project).ToBeVisibleAsync();
    await CheckAboutPortraitAsync();
    await CheckPageWidthAsync($"Homepage at {width}px");
    await skills.ClickAsync();
    await Expect(page.GetByText("Rider", new() { Exact = true })).ToBeVisibleAsync();
    await CheckPageWidthAsync($"Expanded skills at {width}px");
    await skills.ClickAsync();
    if (width == 390)
    {
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Expect(page.Locator("html")).ToHaveCSSAsync("scroll-behavior", "auto");
        if (!await project.EvaluateAsync<bool>("element => getComputedStyle(element).transitionDuration.split(',').every(value => parseFloat(value) <= 0.001)"))
            throw new InvalidOperationException("Project motion did not respect reduced-motion preference.");
        await project.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await page.Locator(".hero-statement").ClickAsync();
        await page.Mouse.MoveAsync(0, 0);
        await page.EvaluateAsync("window.scrollTo({ top: 0, behavior: 'instant' })");
        await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "mobile.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
    }
    await gamerView.ClickAsync();
    await Expect(page.Locator(".portfolio-shell")).ToHaveAttributeAsync("data-view", "gamer");
    await Expect(professionalView).ToBeVisibleAsync();
    await Expect(gamerView).ToBeVisibleAsync();
    var gamerLogo = page.GetByAltText("Monatrox identity logo", new() { Exact = true });
    await Expect(gamerLogo).ToBeVisibleAsync();
    await Expect(gamerLogo).ToHaveCSSAsync("object-fit", "contain");
    await CheckAboutPortraitAsync(gamer: true);
    if (!await gamerLogo.EvaluateAsync<bool>("image => Math.abs(image.getBoundingClientRect().height - image.parentElement.getBoundingClientRect().height) < 1"))
        throw new InvalidOperationException($"Gamer logo frame clips or changes size at {width}px viewport.");
    await CheckPageWidthAsync($"Gamer view at {width}px");
    if (width == 390)
    {
        await page.Locator(".hero-statement").ClickAsync();
        await page.Mouse.MoveAsync(0, 0);
        await page.EvaluateAsync("window.scrollTo({ top: 0, behavior: 'instant' })");
        await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "gamer-mobile.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
    }
    await professionalView.ClickAsync();
    await Expect(page.Locator(".portfolio-shell")).ToHaveAttributeAsync("data-view", "professional");
}
await page.SetViewportSizeAsync(1920, 1080);
await CheckHeaderAsync();

await using var noScript = await browser.NewContextAsync(new() { JavaScriptEnabled = false });
var staticPage = await noScript.NewPageAsync();
await staticPage.GotoAsync(url.ToString());
await Expect(staticPage.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Tools, Skills, and Languages" })).ToBeDisabledAsync();
await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Show Zac Taylor", Exact = true })).ToBeDisabledAsync();
await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Show Monatrox", Exact = true })).ToBeDisabledAsync();
await Expect(staticPage.GetByAltText("Portrait of Zac Taylor", new() { Exact = true })).ToBeVisibleAsync();
await CheckSelectorColorsAsync(staticPage);
await Expect(staticPage.GetByRole(AriaRole.Combobox, new() { Name = "Header design", Exact = true })).ToHaveCountAsync(0);
await Expect(staticPage.Locator(".brand-mark")).ToHaveCSSAsync("border-radius", "50%");
await Expect(staticPage.Locator("head link[rel='icon']")).ToHaveAttributeAsync("href", faviconHref);
await Expect(staticPage.Locator("#about img")).ToHaveAttributeAsync("src", "Content/Images/zac-taylor-about.jpg");
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
await CheckContactAsync();
await CheckProjectPagesAsync();
if (circuitRequests.Count != 0) throw new InvalidOperationException("Unexpected Blazor Server circuit: " + string.Join(", ", circuitRequests));
if (errors.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
Console.WriteLine("WASM smoke passed: prerendered HTML without JS, WebAssembly runtime, no handoff refetch/circuits/autofocus, offline local interactions, permanent avatar-bubble capsule with original white/lime logo in both modes/mobile sizes, stable prerender/interactive/no-JS selector colors, mode-specific About portraits, aligned supporting cards, personal copy without mode labels, identity-first landing screen/stacked selectors/opposed icons/portrait-logo swap/below-fold details, light/dark state-preserving switch, project grid, keyboard/dialog focus, mobile widths/reduced motion, same-origin 404/503 recovery, CCF/Distill/Portfolio case studies with no-JS body and no handoff refetch, scrollable case-study dialogs with focus/scroll restoration and native new-tab links, gamer context, padded workflow tabs with colored indicators, inline Markdown, literal code copy, native disclosures and offline calculator, LinkedIn/GitHub links, Discord clipboard fallback, local-capture-only contact form, validation/draft-preserving failures and throttling.");
return 0;

async Task CheckTextContrastAsync(ILocator element)
{
    var ratio = await element.EvaluateAsync<double>("""
        element => {
            const parse = color => {
                const values = color.match(/[\d.]+/g).map(Number);
                return [values[0] / 255, values[1] / 255, values[2] / 255, values[3] ?? 1];
            };
            const over = (front, back) => [0, 1, 2].map(i => front[i] * front[3] + back[i] * (1 - front[3])).concat(1);
            const parents = [];
            for (let node = element; node; node = node.parentElement) parents.unshift(node);
            let background = [1, 1, 1, 1];
            for (const parent of parents) background = over(parse(getComputedStyle(parent).backgroundColor), background);
            const text = over(parse(getComputedStyle(element).color), background);
            const luminance = color => color.slice(0, 3).map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4)
                .reduce((sum, value, i) => sum + value * [.2126, .7152, .0722][i], 0);
            const light = luminance(text), dark = luminance(background);
            return (Math.max(light, dark) + .05) / (Math.min(light, dark) + .05);
        }
        """);
    if (ratio < 4.5) throw new InvalidOperationException($"Contact text contrast below 4.5:1: {ratio:F2}.");
}

async Task CheckContactAsync()
{
    await page.SetViewportSizeAsync(1366, 900);
    await Expect(page.Locator(".social-links a")).ToHaveCountAsync(2);
    await Expect(page.GetByRole(AriaRole.Link, new() { Name = "LinkedIn", Exact = false })).ToHaveAttributeAsync("href", "https://www.linkedin.com/in/zactaylor-dev");
    await Expect(page.GetByRole(AriaRole.Link, new() { Name = "GitHub", Exact = false })).ToHaveAttributeAsync("href", "https://github.com/Monatrox");
    await Expect(page.GetByText("Steam", new() { Exact = true })).ToHaveCountAsync(0);
    await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
    await page.GetByRole(AriaRole.Button, new() { Name = "Copy Discord username monatrox", Exact = true }).ClickAsync();
    if (await page.EvaluateAsync<string>("navigator.clipboard.readText()") != "monatrox") throw new InvalidOperationException("Discord copy changed username.");
    var email = page.GetByRole(AriaRole.Button, new() { Name = "Email — Contact Me", Exact = true });
    await email.FocusAsync(); await page.Keyboard.PressAsync("Enter");
    var dialog = page.GetByRole(AriaRole.Dialog);
    // Never send a synthetic browser message through a real delivery provider.
    await Expect(dialog.GetByText("Local preview: messages are captured by smtp4dev, not delivered to zac@monatrox.com.", new() { Exact = true })).ToBeVisibleAsync();
    await CheckTextContrastAsync(dialog.Locator(".mud-alert").First);
    await dialog.GetByRole(AriaRole.Button, new() { Name = "Send message", Exact = true }).ClickAsync();
    await Expect(dialog.GetByText("Enter your name.", new() { Exact = true })).ToBeVisibleAsync();
    await CheckTextContrastAsync(dialog.GetByText("Enter your name.", new() { Exact = true }));
    await dialog.GetByLabel("Your name", new() { Exact = true }).FillAsync("Browser verification");
    await dialog.GetByLabel("Your email", new() { Exact = true }).FillAsync("visitor@example.com");
    await dialog.GetByLabel("Message", new() { Exact = true }).FillAsync("Local contact smoke test — " + Guid.NewGuid().ToString("N"));
    foreach (var width in new[] { 1366, 390, 320 })
    {
        await page.SetViewportSizeAsync(width, 900);
        if (!await dialog.EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1 && el.getBoundingClientRect().height <= innerHeight")) throw new InvalidOperationException("Contact dialog overflows.");
        await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, $"contact-{width}.png"), Animations = ScreenshotAnimations.Disabled });
    }
    await dialog.GetByRole(AriaRole.Button, new() { Name = "Send message", Exact = true }).ClickAsync();
    await Expect(dialog.GetByRole(AriaRole.Status)).ToContainTextAsync("Captured locally in smtp4dev. Not delivered to Zac’s inbox.");
    await CheckTextContrastAsync(dialog.GetByRole(AriaRole.Status));
    await dialog.GetByRole(AriaRole.Button, new() { Name = "Done", Exact = true }).ClickAsync();
    await Expect(dialog).ToHaveCountAsync(0);
    await Expect(email).ToBeFocusedAsync();
    await page.SetViewportSizeAsync(1920, 1080);
    await gamerView.ClickAsync();
    await email.ClickAsync();
    await Expect(dialog).ToHaveCSSAsync("background-color", "rgb(35, 39, 32)");
    await Expect(dialog.GetByLabel("Your email", new() { Exact = true })).ToBeVisibleAsync();
    await CheckTextContrastAsync(dialog.Locator(".mud-alert").First);
    await page.ScreenshotAsync(new() { Path = Path.Combine(screenshots, "contact-gamer.png"), Animations = ScreenshotAnimations.Disabled });
    await page.Keyboard.PressAsync("Escape");
    await Expect(dialog).ToHaveCountAsync(0);
    await Expect(email).ToBeFocusedAsync();
    await professionalView.ClickAsync();
    await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Email — Contact Me", Exact = true })).ToBeDisabledAsync();
    await Expect(staticPage.GetByRole(AriaRole.Button, new() { Name = "Copy Discord username monatrox", Exact = true })).ToBeDisabledAsync();

    await using var failure = await browser.NewContextAsync();
    var status = 503;
    await failure.RouteAsync("**/api/contact", route => route.FulfillAsync(new()
    {
        Status = route.Request.Method == "GET" ? 200 : status, ContentType = "application/json",
        Body = route.Request.Method == "GET" ? "{\"available\":true,\"capturedLocally\":true}" : "{}"
    }));
    var target = await failure.NewPageAsync();
    target.PageError += (_, error) => errors.Add(error);
    await target.GotoAsync(url.ToString());
    await Expect(target.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
    await target.GetByRole(AriaRole.Button, new() { Name = "Email — Contact Me", Exact = true }).ClickAsync();
    var failedDialog = target.GetByRole(AriaRole.Dialog);
    await failedDialog.GetByLabel("Your name", new() { Exact = true }).FillAsync("Visitor");
    await failedDialog.GetByLabel("Your email", new() { Exact = true }).FillAsync("visitor@example.com");
    await failedDialog.GetByLabel("Message", new() { Exact = true }).FillAsync("Keep this draft");
    await failedDialog.GetByRole(AriaRole.Button, new() { Name = "Send message", Exact = true }).ClickAsync();
    await Expect(failedDialog.GetByText("Message could not be confirmed. Your draft is still here; please retry later.", new() { Exact = true })).ToBeVisibleAsync();
    await Expect(failedDialog.GetByLabel("Message", new() { Exact = true })).ToHaveValueAsync("Keep this draft");
    status = 429;
    await failedDialog.GetByRole(AriaRole.Button, new() { Name = "Send message", Exact = true }).ClickAsync();
    await Expect(failedDialog.GetByText("Too many attempts. Please wait 10 minutes before trying again.", new() { Exact = true })).ToBeVisibleAsync();
    await Expect(failedDialog.GetByLabel("Message", new() { Exact = true })).ToHaveValueAsync("Keep this draft");
}

async Task CheckProjectPagesAsync()
{
    var directory = Path.Combine(Path.GetTempPath(), "monatrox-project-pages");
    Directory.CreateDirectory(directory);
    await Expect(projects.GetByRole(AriaRole.Link, new() { Name = "Read CCF case study", Exact = true })).ToBeVisibleAsync();
    await CheckCaseStudyModalsAsync(directory);
    foreach (var (id, title) in new[] { ("ccf", "CCF"), ("distill", "Distill"), ("portfolio", "Portfolio") })
    {
        await page.SetViewportSizeAsync(1366, 900);
        var before = browserApiRequests.Count;
        await page.GotoAsync(new Uri(url, "projects/" + id).ToString());
        await Expect(page.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })).ToBeVisibleAsync();
        await Expect(page.Locator(".case-section")).ToHaveCountAsync(5);
        if (browserApiRequests.Count != before) throw new InvalidOperationException("Project page refetched on prerender handoff.");
        if (id == "ccf")
        {
            var tab = page.GetByRole(AriaRole.Tab, new() { Name = "Prepare an event", Exact = true });
            await tab.FocusAsync(); await page.Keyboard.PressAsync("Enter");
            await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
            await Expect(page.Locator(".case-workflow-panel")).ToHaveCSSAsync("padding-left", "24px");
            await Expect(page.Locator(".mud-tab-slider")).ToHaveCSSAsync("background-color", "rgb(82, 107, 23)");
            await page.Locator(".case-tabs").ScreenshotAsync(new() { Path = Path.Combine(directory, "ccf-workflow-professional.png"), Animations = ScreenshotAnimations.Disabled });
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Prepare for a sibling event" })).ToBeVisibleAsync();
            await Expect(page.GetByText("No production records are loaded.", new() { Exact = false })).ToBeVisibleAsync();
        }
        else if (id == "distill")
        {
            await Expect(page.Locator(".case-study a")).ToHaveCountAsync(1);
            await Expect(page.Locator(".case-study a")).ToHaveAttributeAsync("href", "https://github.com/Monatrox/distill");
            await Expect(page.Locator(".case-study")).ToContainTextAsync("Domain contracts");
            await Expect(page.GetByText("99.90%", new() { Exact = true })).ToBeVisibleAsync();
            var slider = page.GetByRole(AriaRole.Slider, new() { Name = "Producer instances", Exact = true });
            await Expect(slider).ToBeEnabledAsync();
            await context.SetOfflineAsync(true);
            try { await slider.FocusAsync(); await page.Keyboard.PressAsync("End"); await Expect(page.GetByText("301 records", new() { Exact = true })).ToBeVisibleAsync(); }
            finally { await context.SetOfflineAsync(false); }
            await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
            var expected = await page.Locator(".case-code pre code").First.InnerTextAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Copy Private admin · POST /channels", Exact = true }).ClickAsync();
            if (await page.EvaluateAsync<string>("navigator.clipboard.readText()") != expected) throw new InvalidOperationException("Code copy changed example.");
            await Expect(page.GetByRole(AriaRole.Status).First).ToHaveTextAsync("Code copied.");
            await page.Locator("summary").First.ClickAsync();
            await Expect(page.Locator("details").First).ToHaveAttributeAsync("open", "");
        }
        else
        {
            await Expect(page.Locator(".case-diagram")).ToHaveCountAsync(3);
            await Expect(page.GetByText("Intentionally overengineered.", new() { Exact = true })).ToBeVisibleAsync();
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Three modules. One API.", Exact = true })).ToBeVisibleAsync();
            await Expect(page.GetByText("Where it's overengineered", new() { Exact = true })).ToBeVisibleAsync();
            await Expect(page.Locator("#development")).ToContainTextAsync("GitHub Actions");
            foreach (var stepTitle in new[] { "Verify", "Publish", "Deploy" })
                await Expect(page.Locator("#development").GetByRole(AriaRole.Heading, new() { Name = stepTitle, Exact = true })).ToBeVisibleAsync();
            await Expect(page.Locator(".case-study")).ToContainTextAsync("Google Workspace");
            await Expect(page.Locator(".case-study")).Not.ToContainTextAsync("No admin UI, sign-in or production email delivery");
            await page.Locator("#boundaries").ScreenshotAsync(new() { Path = Path.Combine(directory, "portfolio-architecture.png"), Animations = ScreenshotAnimations.Disabled });
            await page.Locator("#local-testing").ScreenshotAsync(new() { Path = Path.Combine(directory, "portfolio-local-testing.png"), Animations = ScreenshotAnimations.Disabled });
        }
        foreach (var width in new[] { 1366, 768, 390, 320 })
        {
            await page.SetViewportSizeAsync(width, 900);
            // Chromium can acknowledge viewport changes before responsive layout has painted.
            await page.EvaluateAsync("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
            await CheckPageWidthAsync($"Project {id} at {width}px");
            await page.EvaluateAsync("window.scrollTo({top:0, behavior:'instant'})");
            await page.ScreenshotAsync(new() { Path = Path.Combine(directory, $"{id}-{width}.png"), Animations = ScreenshotAnimations.Disabled });
        }
        await staticPage.GotoAsync(new Uri(url, "projects/" + id).ToString());
        await Expect(staticPage.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })).ToBeVisibleAsync();
        if (id == "ccf") await Expect(staticPage.GetByRole(AriaRole.Heading, new() { Name = "Prepare for a sibling event" })).ToBeVisibleAsync();
        else if (id == "distill") await Expect(staticPage.GetByRole(AriaRole.Slider, new() { Name = "Producer instances", Exact = true })).ToBeDisabledAsync();
        else
        {
            await Expect(staticPage.Locator(".case-diagram")).ToHaveCountAsync(3);
            await Expect(staticPage.Locator("#development")).ToContainTextAsync("GitHub Actions");
            await Expect(staticPage.Locator(".case-study")).ToContainTextAsync("Google Workspace");
        }
    }
    await page.GotoAsync(new Uri(url, "projects/missing").ToString());
    await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("This project page is not available.");
    await using var failure = await browser.NewContextAsync();
    await failure.RouteAsync("**/api/projects/ccf/page", route => route.FulfillAsync(new() { Status = 503, ContentType = "application/problem+json", Body = "{}" }));
    var failedPage = await failure.NewPageAsync();
    failedPage.PageError += (_, error) => errors.Add(error);
    await failedPage.GotoAsync(url.ToString());
    await Expect(failedPage.Locator(".mud-layout")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
    await failedPage.GetByRole(AriaRole.Link, new() { Name = "Read CCF case study", Exact = true }).ClickAsync();
    await Expect(failedPage.GetByRole(AriaRole.Alert)).ToContainTextAsync("Project page is unavailable. Please retry.");
    await failure.UnrouteAsync("**/api/projects/ccf/page");
    await failedPage.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
    await Expect(failedPage.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Heading, new() { Name = "CCF", Exact = true, Level = 1 })).ToBeVisibleAsync();
    if (new Uri(failedPage.Url).AbsolutePath != "/") throw new InvalidOperationException("Dialog retry navigated away from portfolio.");
}

async Task CheckCaseStudyModalsAsync(string directory)
{
    foreach (var name in new[] { "CCF", "Distill", "Portfolio" })
    {
        await page.SetViewportSizeAsync(1366, 900);
        await (name == "CCF" ? gamerView : professionalView).ClickAsync();
        var card = projects.GetByRole(AriaRole.Link, new() { Name = $"Read {name} case study", Exact = true });
        await card.FocusAsync();
        var originalUrl = page.Url;
        var originalTitle = await page.TitleAsync();
        var scrollY = await page.EvaluateAsync<double>("window.scrollY");
        await page.Keyboard.PressAsync("Enter");
        var dialog = page.GetByRole(AriaRole.Dialog);
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true, Level = 1 })).ToBeVisibleAsync();
        if (page.Url != originalUrl) throw new InvalidOperationException("Case-study card navigated instead of opening dialog.");
        await Expect(page).ToHaveTitleAsync(originalTitle);
        await Expect(dialog.Locator(".site-header, .site-footer")).ToHaveCountAsync(0);
        await Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Open full page" })).ToHaveAttributeAsync("target", "_blank");
        var scroll = dialog.Locator(".case-dialog-scroll");
        if (!await scroll.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight")) throw new InvalidOperationException("Case-study dialog has no bounded scroll region.");
        if (name == "CCF")
        {
            await Expect(dialog.Locator(".case-hero-visual > .case-panel")).ToHaveCSSAsync("background-color", "rgb(35, 39, 32)");
            var tab = dialog.GetByRole(AriaRole.Tab, new() { Name = "Prepare an event", Exact = true });
            await tab.FocusAsync(); await page.Keyboard.PressAsync("Enter");
            await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
            await Expect(dialog.Locator(".case-workflow-panel")).ToHaveCSSAsync("padding-left", "24px");
            await Expect(dialog.Locator(".mud-tab-slider")).ToHaveCSSAsync("background-color", "rgb(176, 220, 74)");
            await dialog.Locator(".case-tabs").ScreenshotAsync(new() { Path = Path.Combine(directory, "ccf-workflow-fixed.png"), Animations = ScreenshotAnimations.Disabled });
        }
        else if (name == "Distill")
        {
            var warning = dialog.Locator(".case-callout").Filter(new() { HasText = "The client queue has a separate contract." });
            await Expect(warning.Locator("p")).ToHaveCountAsync(2);
            if (!(await warning.Locator("p").Last.InnerHTMLAsync()).StartsWith("The <code>ILogger</code> provider", StringComparison.Ordinal))
                throw new InvalidOperationException("Distill inline-code sentence split into paragraphs.");
            await warning.ScreenshotAsync(new() { Path = Path.Combine(directory, "distill-inline-fixed.png"), Animations = ScreenshotAnimations.Disabled });
        }
        else
        {
            await Expect(dialog.Locator(".case-diagram")).ToHaveCountAsync(3);
            await Expect(dialog.GetByText("Where it's overengineered", new() { Exact = true })).ToBeVisibleAsync();
        }
        for (var i = 0; i < 5; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            if (!await page.EvaluateAsync<bool>("!!document.activeElement.closest('[role=dialog]')")) throw new InvalidOperationException("Dialog focus escaped.");
        }
        await scroll.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(card).ToBeFocusedAsync();
        var restoredY = await page.EvaluateAsync<double>("window.scrollY");
        if (Math.Abs(restoredY - scrollY) > 1) throw new InvalidOperationException($"Dialog lost portfolio scroll position: {scrollY} -> {restoredY}.");
        await page.Keyboard.PressAsync("Enter");
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true, Level = 1 })).ToBeVisibleAsync();
        foreach (var width in new[] { 1366, 768, 390, 320 })
        {
            await page.SetViewportSizeAsync(width, 900);
            await scroll.EvaluateAsync("el => el.scrollTop = 0");
            if (!await dialog.Locator(".case-dialog-heading h6").EvaluateAsync<bool>("el => el.getBoundingClientRect().height <= parseFloat(getComputedStyle(el).lineHeight) + 1"))
                throw new InvalidOperationException($"{name} dialog title splits across lines at {width}px.");
            if (!await scroll.EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1"))
                throw new InvalidOperationException($"{name} dialog content overflows at {width}px.");
            if (!await dialog.EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1 && el.getBoundingClientRect().height <= innerHeight"))
                throw new InvalidOperationException($"{name} dialog overflows at {width}px.");
            await page.ScreenshotAsync(new() { Path = Path.Combine(directory, $"{name.ToLowerInvariant()}-modal-{width}.png"), Animations = ScreenshotAnimations.Disabled });
        }
        await page.SetViewportSizeAsync(1366, 900);
        await scroll.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Close" })).ToBeInViewportAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(card).ToBeFocusedAsync();
    }
    var ccf = projects.GetByRole(AriaRole.Link, new() { Name = "Read CCF case study", Exact = true });
    var popupTask = context.WaitForPageAsync();
    await ccf.ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });
    var popup = await popupTask;
    popup.PageError += (_, error) => errors.Add(error);
    await Expect(popup.GetByRole(AriaRole.Heading, new() { Name = "CCF", Exact = true })).ToBeVisibleAsync();
    if (new Uri(popup.Url).AbsolutePath != "/projects/ccf") throw new InvalidOperationException("Modified card click lost dedicated page URL.");
    await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    await popup.CloseAsync();
}

async Task CheckPageWidthAsync(string label)
{
    try
    {
        // Responsive layout and collapse animations can settle after viewport/visibility calls complete.
        await page.WaitForFunctionAsync("document.documentElement.scrollWidth <= window.innerWidth + 1", options: new() { Timeout = 3000 });
    }
    catch (TimeoutException error)
    {
        var layout = await page.EvaluateAsync<string>("""
            () => JSON.stringify({ viewport: innerWidth, scrollWidth: document.documentElement.scrollWidth,
                bodyPadding: getComputedStyle(document.body).paddingRight,
                elements: [...document.querySelectorAll('body *')].map(el => ({ tag: el.tagName, class: el.className,
                    text: el.textContent.slice(0, 60), right: el.getBoundingClientRect().right,
                    width: el.getBoundingClientRect().width, font: getComputedStyle(el).font,
                    overflow: getComputedStyle(el).overflowX }))
                    .filter(el => el.right > innerWidth + 1).slice(0, 15) })
            """);
        var directory = Path.Combine("artifacts", "ci-logs");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, $"overflow-{Guid.NewGuid():N}.png"), Animations = ScreenshotAnimations.Disabled });
        throw new InvalidOperationException($"{label} has persistent horizontal overflow. {layout}", error);
    }
}

async Task CheckHeaderAsync()
{
    var requestsBefore = browserApiRequests.Count;
    await Expect(page.Locator(".header-preview, [data-header]")).ToHaveCountAsync(0);
    await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Header design", Exact = true })).ToHaveCountAsync(0);
    await skills.ClickAsync();
    foreach (var gamer in new[] { false, true })
    {
        await (gamer ? gamerView : professionalView).ClickAsync();
        await Expect(page.Locator(".portfolio-shell")).ToHaveAttributeAsync("data-view", gamer ? "gamer" : "professional");
        await Expect(skills).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.Locator(".brand-mark img")).ToBeVisibleAsync();
        await Expect(page.Locator(".brand-mark img")).ToHaveCSSAsync("object-fit", "contain");
        await Expect(page.Locator(".brand-mark img")).ToHaveCSSAsync("filter", "none");
        await Expect(page.Locator(".brand-mark img")).ToHaveAttributeAsync("src", "Content/Images/MX Abstract Logo Cleaned_Letters_DarkTheme.png");
        await Expect(page.Locator(".brand-mark")).ToHaveCSSAsync("background-color", "rgb(37, 43, 35)");
        await Expect(page.Locator(".brand-mark")).ToHaveCSSAsync("border-radius", "50%");
        await Expect(page.Locator(".site-nav a").First).ToHaveCSSAsync("color", gamer ? "rgb(238, 241, 233)" : "rgb(37, 43, 35)");
        await Expect(page.Locator(".brand-wordmark")).ToHaveCSSAsync("color", gamer ? "rgb(238, 241, 233)" : "rgb(37, 43, 35)");
        await Expect(favicon).ToHaveAttributeAsync("href", faviconHref);
        await Expect(page.Locator(".site-nav > a")).ToHaveCountAsync(3);
        await Expect(page.Locator(".site-nav a span")).ToHaveTextAsync(new[] { "01", "02", "03" });
        foreach (var (width, height) in new[] { (1366, 768), (768, 844), (681, 844), (680, 844), (600, 844), (480, 844), (461, 844), (460, 844), (421, 844), (420, 844), (390, 844), (360, 844), (320, 844) })
        {
            await page.SetViewportSizeAsync(width, height);
            await page.EvaluateAsync("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
            await page.EvaluateAsync("window.scrollTo({ top: 0, behavior: 'instant' })");
            if (width <= 420) await Expect(page.Locator(".brand-wordmark")).ToBeHiddenAsync();
            else await Expect(page.Locator(".brand-wordmark")).ToBeVisibleAsync();
            await CheckPageWidthAsync($"Header at {width}px");
            await Expect(page.Locator(".site-header-inner")).ToHaveCSSAsync("border-radius", width <= 680 ? "20px" : "999px");
            if (!await page.Locator(".site-nav").EvaluateAsync<bool>("""
                nav => {
                    const links = [...nav.children].map(link => link.getBoundingClientRect());
                    const gap = innerWidth <= 680 ? 12 : 28;
                    return links.every((box, index) => index === 0 || Math.abs(box.left - links[index - 1].right - gap) <= 1);
                }
                """))
                throw new InvalidOperationException($"Header link spacing must stay fixed, not stretch across the row, at {width}px.");
            if (!await page.Locator(".site-header").EvaluateAsync<bool>("""
                header => {
                    const bounds = header.getBoundingClientRect();
                    const brand = header.querySelector('.site-brand').getBoundingClientRect();
                    const links = [...header.querySelectorAll('.site-nav a')];
                    return brand.width >= 24 && brand.height >= 44 && links.length === 3 && links.every(link => {
                        const box = link.getBoundingClientRect();
                        const index = link.querySelector('span');
                        const number = index.getBoundingClientRect();
                        const label = [...link.childNodes].find(node => node.nodeType === Node.TEXT_NODE && node.textContent.trim());
                        const range = document.createRange();
                        range.selectNodeContents(label);
                        const spacing = number.left - range.getBoundingClientRect().right;
                        const textStyle = getComputedStyle(link), numberStyle = getComputedStyle(index);
                        // Flex items blockify inline-block links; their children still share an inline baseline.
                        return textStyle.display === 'block' && textStyle.fontSize === '13px' &&
                            numberStyle.fontSize === '10px' && numberStyle.verticalAlign === 'baseline' &&
                            Math.abs(spacing - 6) <= 1 && box.width > 0 && box.height > 0 && box.left >= brand.right - 1 && box.right <= bounds.right + 1 &&
                            box.top >= bounds.top - 1 && box.bottom <= bounds.bottom + 1 && number.width > 0 &&
                            Math.abs((box.top + box.bottom - brand.top - brand.bottom) / 2) <= 1 &&
                            box.height >= 44;
                    });
                }
                """))
                throw new InvalidOperationException($"Header must keep logo and numbered navigation on one unclipped row at {width}px.");
            if (width == 1366)
            {
                if (!await page.EvaluateAsync<bool>("document.querySelector('#about').getBoundingClientRect().top >= window.innerHeight - 1"))
                    throw new InvalidOperationException("Header breaks below-fold layout.");
                await page.Locator(".site-header").ScreenshotAsync(new()
                {
                    Path = Path.Combine(screenshots, $"header-avatar-{(gamer ? "gamer" : "professional")}.png"),
                    Animations = ScreenshotAnimations.Disabled
                });
            }
            else if (width is 600 or 480 or 420 or 390 or 320)
            {
                await page.Locator(".site-header").ScreenshotAsync(new()
                {
                    Path = Path.Combine(screenshots, $"header-avatar-{(gamer ? "gamer" : "professional")}-{width}.png"),
                    Animations = ScreenshotAnimations.Disabled
                });
            }
        }
    }
    var wideFont = await page.AddStyleTagAsync(new() { Content = ":root { --font-body: 'DejaVu Sans', sans-serif; }" });
    try
    {
        foreach (var gamer in new[] { false, true })
        {
            await (gamer ? gamerView : professionalView).ClickAsync();
            await Expect(page.Locator(".portfolio-shell")).ToHaveAttributeAsync("data-view", gamer ? "gamer" : "professional");
            foreach (var width in new[] { 480, 461, 460, 421, 360, 320 })
            {
                await page.SetViewportSizeAsync(width, 844);
                await CheckPageWidthAsync($"Wide-system-font header at {width}px");
                if (!await page.Locator(".site-header-inner").EvaluateAsync<bool>("el => el.querySelector('.site-nav').getBoundingClientRect().right <= el.getBoundingClientRect().right + 1"))
                    throw new InvalidOperationException($"Wide-system-font navigation escapes its container at {width}px.");
            }
        }
        await page.Locator(".site-header").ScreenshotAsync(new()
        {
            Path = Path.Combine(screenshots, "header-avatar-wide-font-320.png"),
            Animations = ScreenshotAnimations.Disabled
        });
    }
    finally { await wideFont.EvaluateAsync("element => element.remove()"); }
    await professionalView.ClickAsync();
    await skills.ClickAsync();
    await page.SetViewportSizeAsync(1920, 1080);
    if (browserApiRequests.Count != requestsBefore) throw new InvalidOperationException("Header interactions refetched API data.");
}

async Task CheckAboutPortraitAsync(bool gamer = false)
{
    var image = page.Locator("#about img");
    await image.ScrollIntoViewIfNeededAsync();
    await Expect(image).ToHaveAttributeAsync("src", gamer ? "Content/Images/monatrox-about.jpg" : "Content/Images/zac-taylor-about.jpg");
    await Expect(image).ToHaveAttributeAsync("alt", gamer ? "Monatrox portrait" : "Zac Taylor portrait");
    await Expect(image).ToHaveJSPropertyAsync("complete", true);
    if (!await image.EvaluateAsync<bool>("image => image.naturalWidth > 0 && image.getBoundingClientRect().height >= 150"))
        throw new InvalidOperationException("About portrait must load at readable size.");
    if (page.ViewportSize!.Width > 800)
    {
        var story = await page.Locator(".about-copy").BoundingBoxAsync();
        var photo = await page.Locator(".about-visual").BoundingBoxAsync();
        if (story is null || photo is null || Math.Abs(story.Y - photo.Y) > 1 || Math.Abs(story.Y + story.Height - photo.Y - photo.Height) > 1 || Math.Abs(story.Width - photo.Width) > 1)
            throw new InvalidOperationException("About story and portrait cards must align.");
    }
}

async Task CheckDetailAlignmentAsync(string sectionId)
{
    var aligned = await page.Locator($"#{sectionId} .detail-grid").EvaluateAsync<bool>("""
        grid => {
            const columns = [...grid.children];
            const left = columns[0].querySelector('.detail-panel').getBoundingClientRect();
            const rightPanels = [...columns[1].querySelectorAll('.detail-panel')];
            const first = rightPanels[0].getBoundingClientRect();
            const last = rightPanels.at(-1).getBoundingClientRect();
            return Math.abs(left.top - first.top) <= 1 && Math.abs(left.bottom - last.bottom) <= 1 && Math.abs(left.width - first.width) <= 1;
        }
        """);
    if (!aligned) throw new InvalidOperationException($"Supporting cards must align in {sectionId}.");
}

async Task CheckSelectorColorsAsync(IPage target, bool gamer = false)
{
    await Expect(target.GetByRole(AriaRole.Button, new() { Name = "Show Zac Taylor", Exact = true }).Locator(".identity-name"))
        .ToHaveCSSAsync("color", gamer ? "rgb(187, 196, 177)" : "rgb(37, 43, 35)");
    await Expect(target.GetByRole(AriaRole.Button, new() { Name = "Show Monatrox", Exact = true }).Locator(".identity-name"))
        .ToHaveCSSAsync("color", gamer ? "rgb(238, 241, 233)" : "rgb(98, 105, 92)");
}

async Task CheckPersonalLabelsAsync()
{
    var labels = string.Join(" ", await page.Locator(".site-header, .hero-identity, .identity-visual figcaption, .site-footer").AllInnerTextsAsync());
    if (Regex.IsMatch(labels, @"\b(gamer|professional|portfolio)\b|two perspectives", RegexOptions.IgnoreCase))
        throw new InvalidOperationException("Presentation labels should describe Zac/Monatrox, not modes or portfolio.");
}
