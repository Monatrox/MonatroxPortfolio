namespace MonatroxPortfolio.Testing;

public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"monatroxportfolio-{Guid.NewGuid():N}");
    public string ConnectionString => $"Data Source={System.IO.Path.Combine(Path, "portfolio.db")};Pooling=False;Foreign Keys=True";

    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
