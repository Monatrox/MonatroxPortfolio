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
page.SetDefaultTimeout(15000);
await page.GotoAsync(url.ToString());
await Expect(page.GetByText("Team Lead at Falkon Technologies", new() { Exact = true })).ToBeVisibleAsync();
await Expect(page.GetByAltText("MX logo")).ToBeVisibleAsync();
await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(24, 24, 24)");
var skills = page.GetByRole(AriaRole.Button, new() { Name = "Tools, Skills, and Languages" });
await Expect(skills).ToBeEnabledAsync();
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
if (errors.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
Console.WriteLine("Browser smoke passed: imported homepage without heading autofocus, skills/background toggles, keyboard dialog, placeholders, carousel; no page errors.");
return 0;
