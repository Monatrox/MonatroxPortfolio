using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MonatroxPortfolio.Projects.Public;

namespace MonatroxPortfolio.Projects.Application.Features.Content;

internal static class ProjectDocumentValidation
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().DisableHtml().UsePipeTables().Build();

    public static void Validate(ProjectDocumentDto document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.SchemaVersion != 1) throw new ArgumentException("Unsupported project page schema version.");
        ArgumentNullException.ThrowIfNull(document.Hero);
        Text(document.Hero.Eyebrow); Text(document.Hero.Title); Text(document.Hero.Subtitle);
        MarkdownText(document.Hero.IntroMarkdown);
        if (document.Hero.Context is not null) Text(document.Hero.Context);
        foreach (var tag in Items(document.Hero.Tags, 20, true)) Text(tag);
        var count = 0;
        Blocks(document.Hero.Visual, 0, ref count);
        if (document.Introduction is not null) Blocks(document.Introduction, 0, ref count);
        foreach (var fact in Items(document.Overview, 6, true)) { Text(fact.Title); MarkdownText(fact.Markdown); }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in Items(document.Sections, 30))
        {
            Text(section.Id);
            if (section.Id.Length > 80 || !section.Id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') || !ids.Add(section.Id))
                throw new ArgumentException("Section IDs must be unique ASCII anchors.");
            Text(section.Title); MarkdownText(section.IntroMarkdown, true);
            Blocks(section.Blocks, 0, ref count);
        }
        foreach (var note in Items(document.Notes, 30, true)) MarkdownText(note);
    }

    private static void Blocks(ProjectBlockDto[] blocks, int depth, ref int count)
    {
        if (depth > 6) throw new ArgumentException("Project blocks exceed maximum nesting depth.");
        foreach (var block in Items(blocks, 100, true))
        {
            if (++count > 400) throw new ArgumentException("Project page has too many blocks.");
            switch (block)
            {
                case MarkdownBlockDto text:
                    MarkdownText(text.Markdown); break;
                case CardsBlockDto cards:
                    Columns(cards.Columns);
                    foreach (var card in Items(cards.Cards, 30))
                    {
                        Text(card.Title); MarkdownText(card.Markdown);
                        if (card.Eyebrow is not null) Text(card.Eyebrow);
                        if (card.FooterMarkdown is not null) MarkdownText(card.FooterMarkdown);
                    }
                    break;
                case ColumnsBlockDto columns:
                    Columns(columns.Columns); Blocks(columns.Blocks, depth + 1, ref count); break;
                case PanelBlockDto panel:
                    Tone(panel.Tone); if (panel.Title is not null) Text(panel.Title);
                    Blocks(panel.Blocks, depth + 1, ref count); break;
                case TableBlockDto table:
                    Text(table.Caption);
                    foreach (var header in Items(table.Headers, 8)) Text(header);
                    foreach (var row in Items(table.Rows, 100))
                    {
                        if (row.Length != table.Headers.Length) throw new ArgumentException("Table rows must match headers.");
                        foreach (var cell in row) MarkdownText(cell, true);
                    }
                    break;
                case CodeBlockDto code:
                    Text(code.Title); Text(code.Code);
                    if (code.Language is not ("text" or "json" or "csharp" or "shell" or "rust"))
                        throw new ArgumentException("Unsupported code language.");
                    if (code.CaptionMarkdown is not null) MarkdownText(code.CaptionMarkdown);
                    break;
                case CalloutBlockDto callout:
                    Tone(callout.Tone); MarkdownText(callout.Markdown);
                    if (callout.Title is not null) Text(callout.Title);
                    break;
                case StepsBlockDto steps:
                    Tone(steps.Tone);
                    foreach (var step in Items(steps.Steps, 12))
                    {
                        Text(step.Title); MarkdownText(step.Markdown);
                        if (step.Eyebrow is not null) Text(step.Eyebrow);
                    }
                    if (steps.CaptionMarkdown is not null) MarkdownText(steps.CaptionMarkdown);
                    break;
                case DiagramBlockDto diagram:
                    if (diagram.Layout is not ("chain" or "branch")) throw new ArgumentException("Unsupported diagram layout.");
                    foreach (var node in Items(diagram.Nodes, 6))
                    {
                        Text(node.Title); MarkdownText(node.Markdown, true); Tone(node.Tone);
                        if (node.Eyebrow is not null) Text(node.Eyebrow);
                    }
                    if (diagram.CaptionMarkdown is not null) MarkdownText(diagram.CaptionMarkdown);
                    break;
                case TabsBlockDto tabs:
                    foreach (var tab in Items(tabs.Tabs, 8))
                    { Text(tab.Title); Blocks(tab.Blocks, depth + 1, ref count); }
                    if (tabs.CaptionMarkdown is not null) MarkdownText(tabs.CaptionMarkdown);
                    break;
                case DetailsBlockDto details:
                    Text(details.Summary); Blocks(details.Blocks, depth + 1, ref count); break;
                case CalculatorBlockDto calculator:
                    MarkdownText(calculator.AssumptionsMarkdown);
                    if (calculator.Instances is < 1 or > 200 || calculator.Rate is < 1 or > 500 ||
                        calculator.WindowSeconds is < 10 or > 300 || calculator.WindowSeconds % 10 != 0 ||
                        calculator.ErrorRate is < 0 or > 1)
                        throw new ArgumentException("Calculator defaults are outside supported bounds.");
                    break;
                default: throw new ArgumentException("Unsupported project block.");
            }
        }
    }

    private static IEnumerable<T> Items<T>(T[]? values, int maximum, bool allowEmpty = false)
    {
        if (values is null || values.Length > maximum || (!allowEmpty && values.Length == 0))
            throw new ArgumentException("Invalid project content collection size.");
        foreach (var item in values)
        {
            if (item is null) throw new ArgumentException("Project content items cannot be null.");
            yield return item;
        }
    }

    private static void Text(string? value, bool allowEmpty = false)
    {
        if (value is null || value.Length > 100_000 || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
            throw new ArgumentException("Invalid project content text.");
    }

    private static void Columns(int columns)
    {
        if (columns is < 1 or > 3) throw new ArgumentException("Content supports one to three columns.");
    }

    private static void Tone(string tone)
    {
        if (tone is not ("plain" or "surface" or "accent" or "dark" or "note" or "warning"))
            throw new ArgumentException("Unsupported content tone.");
    }

    private static void MarkdownText(string markdown, bool allowEmpty = false)
    {
        Text(markdown, allowEmpty);
        var parsed = Markdown.Parse(markdown, MarkdownPipeline);
        foreach (var link in parsed.Descendants<LinkInline>()) Link(link.Url);
        foreach (var link in parsed.Descendants<AutolinkInline>()) Link(link.Url);
    }

    private static void Link(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Any(char.IsControl) || url.StartsWith("//", StringComparison.Ordinal) || url.Contains('\\') ||
            (Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Scheme is not ("http" or "https") : url.Contains(':')))
            throw new ArgumentException("Project Markdown links must use safe HTTP(S) or relative references.");
    }
}
