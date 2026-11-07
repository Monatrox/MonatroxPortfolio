using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonatroxPortfolio.Profile.Infrastructure;
using MonatroxPortfolio.Projects.Infrastructure;
using MonatroxPortfolio.Shared.Contracts;

if (args.Length != 0 && (args.Length != 2 || args[0] != "--import"))
{
    Console.Error.WriteLine("Usage: MonatroxPortfolio.Migrations [--import <content-directory>]");
    return 1;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    var builder = Host.CreateApplicationBuilder();
    var connection = builder.Configuration.GetConnectionString("Portfolio")
        ?? throw new InvalidOperationException("Set ConnectionStrings__Portfolio to an explicit SQLite connection string.");
    builder.Services.AddProfile(connection);
    builder.Services.AddProjects(connection);
    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    var prepared = new List<(IContentImporter Importer, string Json)>();
    if (args.Length == 2)
    {
        foreach (var importer in scope.ServiceProvider.GetServices<IContentImporter>())
        {
            var json = await File.ReadAllTextAsync(Path.Combine(args[1], importer.FileName), cancellation.Token);
            var validation = importer.Validate(json);
            if (!validation.IsSuccess) throw new InvalidOperationException($"{importer.FileName}: {validation.Error!.Message}");
            prepared.Add((importer, json));
        }
    }
    foreach (var migration in scope.ServiceProvider.GetServices<IModuleMigration>())
        await migration.MigrateAsync(cancellation.Token);
    foreach (var (importer, json) in prepared)
    {
        var result = await importer.ImportAsync(json, cancellation.Token);
        if (!result.IsSuccess) throw new InvalidOperationException($"{importer.FileName}: {result.Error!.Message}");
        Console.WriteLine($"Imported {importer.FileName}: {result.Value} root record(s).");
    }
    Console.WriteLine("Migrations complete. No unlisted content deleted.");
    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Canceled. A module already committed may remain imported; rerun to resume.");
    return 130;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration/import failed ({ex.GetType().Name}): {ex.Message}");
    Console.Error.WriteLine("No reset attempted. Module commits are independent; fix input/storage and rerun.");
    return 1;
}
