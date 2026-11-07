namespace MonatroxPortfolio.Projects.Domain;

public sealed class PortfolioProject(string id, string name, string description, string thumbnailSrc, string? link, int sortOrder)
{
    public string Id { get; private set; } = !string.IsNullOrWhiteSpace(id) && id.Length <= 100 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
        ? id : throw new ArgumentException("Project IDs must contain 1–100 ASCII letters, digits, dashes or underscores.");
    public string Name { get; private set; } = Required(name);
    public string Description { get; private set; } = Required(description);
    public string ThumbnailSrc { get; private set; } = SafeReference(thumbnailSrc);
    public string? Link { get; private set; } = link is null ? null : SafeReference(link);
    public int SortOrder { get; private set; } = sortOrder >= 0 ? sortOrder : throw new ArgumentException("Sort order cannot be negative.");

    private static string Required(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 10000
        ? value : throw new ArgumentException("Project fields must contain 1–10000 characters.");

    private static string SafeReference(string value)
    {
        Required(value);
        if (value.Any(char.IsControl) || value.StartsWith("//", StringComparison.Ordinal) || value.Contains('\\'))
            throw new ArgumentException("Invalid project URL.");
        if (value.StartsWith('/') || (Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.Scheme is "http" or "https" : !value.Contains(':')))
            return value;
        throw new ArgumentException("Project URLs must be HTTP(S) or local relative references.");
    }
}
