# MonatroxPortfolio

.NET 10 modular monolith API, prerendered Blazor WebAssembly/MudBlazor frontend, EF Core/SQLite, Aspire + Podman and smtp4dev. Current work: [docs/wasm-migration.md](docs/wasm-migration.md). Original baseline: [docs/v1-plan.md](docs/v1-plan.md).

## Run locally

Prerequisites: SDK **10.0.114** selected by `global.json`, ASP.NET Core/.NET runtime **10.0.14**, working rootless Podman, NuGet/image registry access. Aspire's AppHost SDK supplies orchestration tooling; a separate Aspire CLI installation is not required by these commands.

If using the user-local SDK installed during migration, activate it for this terminal first:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version  # 10.0.114 (or compatible newer patch)
```

In Rider, select `~/.dotnet/dotnet` as .NET CLI executable under **Build, Execution, Deployment → Toolset and Build**. System packages and permanent shell configuration were not changed.

From repository root:

```bash
dotnet tool restore
bash scripts/dev.sh import  # Explicit first-use/content-update operation; also applies migrations.
bash scripts/dev.sh         # Aspire: migrations -> API -> Blazor; smtp4dev alongside.
```

- Portfolio: http://127.0.0.1:5420
- Browser API gateway: http://127.0.0.1:5420/api/profile and `/api/projects`, `/api/projects/{id}`
- Independently hosted API: http://127.0.0.1:5410/api/profile
- smtp4dev inbox: http://127.0.0.1:5430
- Aspire dashboard: http://127.0.0.1:15400 (login link printed in AppHost output)

All published development endpoints are loopback-only, including Podman container bindings. SMTP port is allocated dynamically; Aspire shows it and supplies `Smtp:Host`/`Smtp:Port` to API. No application sends mail yet. smtp4dev's inbox is disposable; no persistent mail volume, outbound relay or production email service is configured.

Normal startup applies migrations only. It **does not import content**. An unpopulated profile displays an import instruction; an API outage displays an error with Retry. `/health` and `/alive` check process health, not database readiness or imported content. Ctrl+C stops Aspire's managed processes/containers.

Frontend host supplies initial HTML via HTTP API reads; UI then runs in browser using WebAssembly. Public profile/projects/error state is transferred into WASM startup, avoiding duplicate handoff fetches and loading flicker. No Blazor Server circuits or Auto mode. Subsequent browser API calls use same-origin `/api` forwarding; no CORS or internal API address in browser configuration. Local UI interaction works after startup without API availability, but data operations still need API.

SQLite lives at `data/portfolio.db`, ignored by Git. To use another existing data directory, supply an **absolute** SQLite connection string via `ConnectionStrings__Portfolio`; both scripts and AppHost respect it. Create its parent directory first. Do not run concurrent migration/import commands.

### Content updates

Edit [content/profile.json](content/profile.json) and [content/projects.json](content/projects.json), then run:

```bash
bash scripts/dev.sh import
```

Imports validate both documents before writes. Unknown fields, missing required fields, duplicate IDs, invalid categories and unsafe URL schemes are rejected. Stable IDs update existing rows; new IDs add rows. Omitted rows/collection items are **not deleted**. Changing an ID adds a new record rather than renaming the old one. Each module commits independently; a later storage failure can leave an earlier module imported. Fix the problem and rerun; back up data before operational changes.

Project link/thumbnail references accept HTTP(S), relative paths, and root-relative web paths such as `/images/project.png`. Network-path URLs (`//host`), backslashes, control characters, and file/script schemes are rejected.

Provided content deliberately preserves the old source's biography, 44 skills, career/education, social `#` links and five dummy projects. Project detail image/description/additional-information sections remain visual placeholders, not completed content. No old databases or Identity data were imported.

### Run without containers

After explicit import, use separate terminals from repository root:

```bash
export ConnectionStrings__Portfolio="Data Source=$PWD/data/portfolio.db;Foreign Keys=True"
dotnet run --project Hosts/MonatroxPortfolio.Api
```

```bash
dotnet run --project Hosts/MonatroxPortfolio.Blazor
```

Launch profiles supply loopback URLs and frontend API address. Without launch profiles, configure `ASPNETCORE_URLS` and `Api__BaseUrl` explicitly. `Api__BaseUrl` must be backend HTTP(S) origin, without credentials, path, query or fragment. Standalone commands do not start smtp4dev or export telemetry to Aspire.

### Verify published Release output

With API running and local SDK/runtime activated:

```bash
dotnet publish Hosts/MonatroxPortfolio.Blazor -c Release -o "$PWD/artifacts/wasm-publish"
Api__BaseUrl=http://127.0.0.1:5410 dotnet artifacts/wasm-publish/MonatroxPortfolio.Blazor.dll \
  --urls http://127.0.0.1:5422 --contentRoot "$PWD/artifacts/wasm-publish"
```

Then run browser smoke against `http://127.0.0.1:5422`. Release retains managed trimming; public DTO assemblies preserve JSON reflection metadata for HTTP and prerender handoff. AOT is not enabled. Published output stays ignored by Git.

## Verify locally

```bash
bash scripts/verify.sh
```

Restores/builds solution, runs architecture/module/API/gateway/CLI tests against isolated test services and temporary SQLite files, and checks both EF models match migrations. It does not mutate development content. No GitHub Actions/deployment configured.

For browser smoke, start imported app, install Chromium once if needed, then:

```bash
dotnet run --project Tests/MonatroxPortfolio.BrowserTests -- --install
PORTFOLIO_BROWSER_URL=http://127.0.0.1:5420 bash scripts/verify.sh
```

Or run just browser smoke:

```bash
dotnet run --project Tests/MonatroxPortfolio.BrowserTests -- http://127.0.0.1:5420
```

Browser check expects the supplied sample content. Covers HTML content without JavaScript, WASM runtime, no startup API refetch or Server circuits, dark background, no heading autofocus, skills/background toggles, keyboard dialog, placeholders, carousel, offline local interactions and same-origin 404/503 recovery through Retry. On Nobara, Playwright warns and uses its Ubuntu fallback build; local smoke passed. No official Playwright OS-support, screenshot-perfect/mobile/production acceptance claim.

## Architecture

- `Modules/Profile` and `Modules/Projects`: Application, Domain, Infrastructure, Public, Tests.
- Endpoints/use cases/validation/Public implementations belong to **Application**; EF mappings, contexts, migrations and persistence implementations belong to **Infrastructure**.
- Cross-module access uses only provider **Public** projects. Domain/Public remain framework-free. No foreign table access or shared business entity library.
- API host composes modules. `Hosts/MonatroxPortfolio.Blazor` owns HTML shell, prerender HTTP registration and transport-only YARP gateway; it references client project and shared service defaults. `Hosts/MonatroxPortfolio.Blazor.Client` owns UI/typed HTTP client and references only module Public contracts. Client contains no server-framework/service-default/database dependencies. Frontend never accesses backend implementations.
- Each module has its own prefixed tables and migration history (`__ProfileMigrations`, `__ProjectsMigrations`) in one file. API/frontend startup never applies migrations.
- `Shared` contains technical contracts and `MonatroxPortfolio.ServiceDefaults` health/telemetry/discovery helpers. Migration/import interfaces are host tooling, not module-to-module business APIs.
- Profile collections have read-only views and guarded domain add methods. EF hydrates their backing fields; imports update/add matching IDs without exposing mutable collections.
- No required generic repository, mediator, messaging broker or distributed cache.

### Add a module migration

```bash
export ConnectionStrings__Portfolio="Data Source=$PWD/data/portfolio.db;Foreign Keys=True"
dotnet tool restore
dotnet ef migrations add YourChange --project Modules/Profile/MonatroxPortfolio.Profile.Infrastructure --output-dir Persistence/Migrations
bash scripts/dev.sh migrate
```

Use Projects.Infrastructure for project schema changes. Do not use `EnsureCreated`, reset databases on startup, or fold module migrations into API host.

## Dependency notes

SDK/package/image pins are versioned. SDK 10.0.114 and runtime 10.0.14 support patched Blazor WASM packages/browser runtime 10.0.14 and Release linker. Frontend host requires runtime 10.0.14; EF remains pinned to 10.0.9, MudBlazor to 9.11.0, Aspire to 13.6.0 and YARP to 2.3.0. Explicit patched native SQLite and MessagePack dependencies address upstream transitive advisories; warnings are not suppressed. Review/update pins and audit dependencies before deployment. Professional/gamer redesign, diagonal identity selector, authentication/admin, Contact Me behavior and production hosting remain future work.
