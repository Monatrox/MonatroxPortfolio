namespace MonatroxPortfolio.Shared.Contracts;

public interface IContentImporter
{
    string FileName { get; }
    Result<int> Validate(string json);
    Task<Result<int>> ImportAsync(string json, CancellationToken cancellationToken = default);
}
