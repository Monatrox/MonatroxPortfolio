namespace MonatroxPortfolio.Profile.Domain;

public sealed class PortfolioProfile(string alias, string name, string headline, string biography)
{
    public string Id { get; private set; } = "main";
    public string Alias { get; private set; } = Content.Required(alias);
    public string Name { get; private set; } = Content.Required(name);
    public string Headline { get; private set; } = Content.Required(headline);
    public string Biography { get; private set; } = Content.Required(biography);
    private readonly List<SocialLink> _socialLinks = [];
    private readonly List<Skill> _skills = [];
    private readonly List<CareerEntry> _career = [];
    private readonly List<EducationEntry> _education = [];

    public IReadOnlyList<SocialLink> SocialLinks => _socialLinks.AsReadOnly();
    public IReadOnlyList<Skill> Skills => _skills.AsReadOnly();
    public IReadOnlyList<CareerEntry> Career => _career.AsReadOnly();
    public IReadOnlyList<EducationEntry> Education => _education.AsReadOnly();

    public void AddSocialLink(SocialLink item) => Content.AddUnique(_socialLinks, item, x => x.Id);
    public void AddSkill(Skill item) => Content.AddUnique(_skills, item, x => x.Id);
    public void AddCareerEntry(CareerEntry item) => Content.AddUnique(_career, item, x => x.Id);
    public void AddEducationEntry(EducationEntry item) => Content.AddUnique(_education, item, x => x.Id);
}

public sealed class SocialLink(string id, string name, string url, int sortOrder)
{
    public string Id { get; private set; } = Content.Required(id);
    public string Name { get; private set; } = Content.Required(name);
    public string Url { get; private set; } = Content.Link(url);
    public int SortOrder { get; private set; } = Content.Order(sortOrder);
}

public sealed class Skill(string id, string name, string category, int sortOrder)
{
    public string Id { get; private set; } = Content.Required(id);
    public string Name { get; private set; } = Content.Required(name);
    public string Category { get; private set; } = category is "DotNet" or "Languages" or "Architecture" or "Data" or "Platforms"
        or "Workflow" or "Business" or "AI" or "Games" or "Tools" or "Skills"
        ? category : throw new ArgumentException("Unknown skill category.");
    public int SortOrder { get; private set; } = Content.Order(sortOrder);
}

public sealed class CareerEntry(string id, string period, string roles, string organization, string location, int sortOrder)
{
    public string Id { get; private set; } = Content.Required(id);
    public string Period { get; private set; } = Content.Required(period);
    public string Roles { get; private set; } = Content.Required(roles);
    public string Organization { get; private set; } = Content.Required(organization);
    public string Location { get; private set; } = Content.Required(location);
    public int SortOrder { get; private set; } = Content.Order(sortOrder);
}

public sealed class EducationEntry(string id, string qualifications, string institution, string location, int sortOrder)
{
    public string Id { get; private set; } = Content.Required(id);
    public string Qualifications { get; private set; } = Content.Required(qualifications);
    public string Institution { get; private set; } = Content.Required(institution);
    public string Location { get; private set; } = Content.Required(location);
    public int SortOrder { get; private set; } = Content.Order(sortOrder);
}

internal static class Content
{
    public static string Required(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 10000
        ? value : throw new ArgumentException("Content fields must contain 1–10000 characters.");
    public static int Order(int value) => value >= 0 ? value : throw new ArgumentException("Sort order cannot be negative.");
    public static string Link(string value) => value == "#" || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
        ? value : throw new ArgumentException("Social links must be HTTP(S) URLs or # placeholders.");
    public static void AddUnique<T>(List<T> items, T item, Func<T, string> id) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        if (items.Any(existing => string.Equals(id(existing), id(item), StringComparison.Ordinal)))
            throw new ArgumentException("Content IDs must be unique within each collection.", nameof(item));
        items.Add(item);
    }
}
