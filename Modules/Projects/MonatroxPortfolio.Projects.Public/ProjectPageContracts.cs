using System.Text.Json.Serialization;

namespace MonatroxPortfolio.Projects.Public;

public sealed record ProjectPageDto(string ProjectId, ProjectDocumentDto Document);
public sealed record ProjectDocumentDto(int SchemaVersion, ProjectHeroDto Hero, ProjectFactDto[] Overview,
    ProjectSectionDto[] Sections, string[] Notes, ProjectBlockDto[]? Introduction = null);
public sealed record ProjectHeroDto(string Eyebrow, string Title, string Subtitle, string IntroMarkdown,
    string[] Tags, ProjectBlockDto[] Visual, string? Context = null);
public sealed record ProjectFactDto(string Title, string Markdown);
public sealed record ProjectSectionDto(string Id, string Title, string IntroMarkdown, ProjectBlockDto[] Blocks);
public sealed record ProjectImportDto(string Id, string Name, string Description, string ThumbnailSrc,
    string? Link, int SortOrder, ProjectDocumentDto? Page = null);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MarkdownBlockDto), "markdown")]
[JsonDerivedType(typeof(CardsBlockDto), "cards")]
[JsonDerivedType(typeof(ColumnsBlockDto), "columns")]
[JsonDerivedType(typeof(PanelBlockDto), "panel")]
[JsonDerivedType(typeof(TableBlockDto), "table")]
[JsonDerivedType(typeof(CodeBlockDto), "code")]
[JsonDerivedType(typeof(CalloutBlockDto), "callout")]
[JsonDerivedType(typeof(StepsBlockDto), "steps")]
[JsonDerivedType(typeof(DiagramBlockDto), "diagram")]
[JsonDerivedType(typeof(TabsBlockDto), "tabs")]
[JsonDerivedType(typeof(DetailsBlockDto), "details")]
[JsonDerivedType(typeof(CalculatorBlockDto), "calculator")]
public abstract record ProjectBlockDto;
public sealed record MarkdownBlockDto(string Markdown) : ProjectBlockDto;
public sealed record ProjectCardDto(string Title, string Markdown, string? Eyebrow = null, string? FooterMarkdown = null);
public sealed record CardsBlockDto(ProjectCardDto[] Cards, int Columns = 2) : ProjectBlockDto;
public sealed record ColumnsBlockDto(ProjectBlockDto[] Blocks, int Columns = 2) : ProjectBlockDto;
public sealed record PanelBlockDto(ProjectBlockDto[] Blocks, string? Title = null, string Tone = "surface") : ProjectBlockDto;
public sealed record TableBlockDto(string Caption, string[] Headers, string[][] Rows) : ProjectBlockDto;
public sealed record CodeBlockDto(string Title, string Language, string Code, string? CaptionMarkdown = null) : ProjectBlockDto;
public sealed record CalloutBlockDto(string Markdown, string Tone = "note", string? Title = null) : ProjectBlockDto;
public sealed record ProjectStepDto(string Title, string Markdown, string? Eyebrow = null);
public sealed record StepsBlockDto(ProjectStepDto[] Steps, string? CaptionMarkdown = null, string Tone = "plain") : ProjectBlockDto;
public sealed record ProjectNodeDto(string Title, string Markdown, string? Eyebrow = null, string Tone = "plain");
public sealed record DiagramBlockDto(ProjectNodeDto[] Nodes, string Layout = "chain", string? CaptionMarkdown = null) : ProjectBlockDto;
public sealed record ProjectTabDto(string Title, ProjectBlockDto[] Blocks);
public sealed record TabsBlockDto(ProjectTabDto[] Tabs, string? CaptionMarkdown = null) : ProjectBlockDto;
public sealed record DetailsBlockDto(string Summary, ProjectBlockDto[] Blocks) : ProjectBlockDto;
public sealed record CalculatorBlockDto(string AssumptionsMarkdown, int Instances = 40, int Rate = 25,
    int WindowSeconds = 60, decimal ErrorRate = 0.001m) : ProjectBlockDto;
