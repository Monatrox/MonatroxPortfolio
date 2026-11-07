namespace MonatroxPortfolio.Shared.Contracts;

public interface IModuleMigration
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
