namespace MonatroxPortfolio.Projects.Domain;

public sealed class ProjectPage(string projectId, string documentJson)
{
    public string ProjectId { get; private set; } = !string.IsNullOrWhiteSpace(projectId) && projectId.Length <= 100 &&
        projectId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
        ? projectId : throw new ArgumentException("Invalid project page ID.");
    // Application validates the typed document; infrastructure stores its canonical JSON unchanged.
    public string DocumentJson { get; private set; } = !string.IsNullOrWhiteSpace(documentJson) && documentJson.Length <= 512_000
        ? documentJson : throw new ArgumentException("Project page must contain 1–512000 characters.");
}
