# MonatroxPortfolio

My personal portfolio, intentionally overengineered as a working demonstration of .NET Aspire and a modular monolith.

A prerendered Blazor WebAssembly frontend calls a single API with Profile, Projects and Contact modules. Each module owns its application, domain and infrastructure code, sharing only public contracts across module boundaries. EF Core/SQLite, Aspire orchestration, automated tests and container-based CI/CD support the example.

[Live portfolio](https://monatrox.com) · [Architecture case study](https://monatrox.com/projects/portfolio) · [Artwork sources](CREDITS.md)
