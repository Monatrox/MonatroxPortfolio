# MonatroxPortfolio

.NET 10 modular monolith API, prerendered Blazor WebAssembly/MudBlazor frontend, EF Core/SQLite, Aspire + Podman and smtp4dev. Project-page authoring: [docs/project-pages.md](docs/project-pages.md). Contact/email behavior: [docs/contact.md](docs/contact.md). Current design: [docs/professional-design.md](docs/professional-design.md); accepted header uses original white/lime logo in avatar bubble inside floating capsule. CI/CD and production bootstrap: [docs/deployment.md](docs/deployment.md). Hosting milestone: [docs/wasm-migration.md](docs/wasm-migration.md). Original baseline: [docs/v1-plan.md](docs/v1-plan.md).

## Branch policy

Use **`develop`** for integration and branch new features from it. It contains the skills grouping and final [Neon Flatline gamer theme](docs/gamer-theme.md). Completed features merge into `develop`; do not merge or push to **`main`** without explicit release approval—main pushes can deploy the live site.

The workflow definition on `develop` permits **main pushes only**, with no PR/manual triggers. Keeping the definition preserves CI/CD during an eventual approved release merge, without running pipelines on develop. Main's existing workflow is unchanged until that merge; see [deployment details](docs/deployment.md#branch-and-trigger-policy).

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
- Browser API gateway: http://127.0.0.1:5420/api/profile and `/api/projects`, `/api/projects/{id}`, `/api/projects/{id}/page`
- Independently hosted API: http://127.0.0.1:5410/api/profile
- smtp4dev inbox: http://127.0.0.1:5430
- Aspire dashboard: http://127.0.0.1:15400 (login link printed in AppHost output)

All published development endpoints are loopback-only, including Podman container bindings. SMTP port is allocated dynamically; Aspire shows it and supplies `Smtp:Host`/`Smtp:Port` to API. Contact Me sends fixed-recipient messages to smtp4dev in local capture mode; **these are not delivered to zac@monatrox.com**. Form explicitly identifies capture mode. Development inbox is disposable and has no outbound relay. Production runs separate Docker-hosted frontend/API services behind Nginx and delivers contact mail through Google Workspace SMTP; credentials stay in the server's root-only environment file.

Normal startup applies migrations only. It **does not import content**. An unpopulated profile displays an import instruction; an API outage displays an error with Retry. `/health` and `/alive` check process health, not database readiness or imported content. Ctrl+C stops Aspire's managed processes/containers.

Frontend host supplies initial HTML via HTTP API reads; UI then runs in browser using WebAssembly. Public profile/projects/error state is transferred into WASM startup, avoiding duplicate handoff fetches and loading flicker. No Blazor Server circuits or Auto mode. Subsequent browser API calls use same-origin `/api` forwarding; no CORS or internal API address in browser configuration. Local UI interaction works after startup without API availability, but data operations still need API.

SQLite lives at `data/portfolio.db`, ignored by Git. To use another existing data directory, supply an **absolute** SQLite connection string via `ConnectionStrings__Portfolio`; both scripts and AppHost respect it. Create its parent directory first. Do not run concurrent migration/import commands.

### Content updates

Edit [content/profile.json](content/profile.json), legacy [content/projects.json](content/projects.json), or individual `content/project-pages/*.json` case studies. New pages start from `content/project-page.template.json`; [authoring guide](docs/project-pages.md) covers blocks, schema/editor help and evidence notes. Then run:

```bash
bash scripts/dev.sh validate  # No database opened/modified.
bash scripts/dev.sh import
```

Imports validate every module document and discovered project file before writes. Unknown fields, missing required fields, duplicate IDs, invalid categories and unsafe URL schemes are rejected. Stable IDs update existing rows; new IDs add rows. Omitted rows/collection items are **not deleted**. Changing an ID adds a new record rather than renaming the old one. Each module commits independently; a later storage failure can leave an earlier module imported. Fix the problem and rerun; back up data before operational changes.

Project link/thumbnail references accept HTTP(S), relative paths, and root-relative web paths such as `/images/project.png`. Network-path URLs (`//host`), backslashes, control characters, and file/script schemes are rejected.

Provided content preserves the old source's biography, 44 skills, career/education. LinkedIn/GitHub use supplied real URLs; Discord shows `monatrox` with Copy rather than a guessed profile link. Email opens validated Contact Me modal. Five dummy projects and their placeholder dialog were removed; CCF, Distill and this portfolio's architecture demo are published. Importer regression fixtures remain test-only. Distill is self-contained, with in-document setup examples and one repository-root link rather than file links. CCF and Distill have reference-derived draft case studies in scrollable homepage dialogs, with the same shared content available at dedicated URLs for direct/new-tab access; claims/evidence are author-supplied, not independently verified. No old databases or Identity data were imported.

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

Restores/builds solution, runs architecture/module/API/gateway/CLI tests against isolated test services and temporary SQLite files, and checks both EF models match migrations. It does not mutate development content. The workflow definition in this branch runs these checks plus published-container browser smoke only on main pushes; develop is checked locally. Successful current-main builds publish commit-tagged images and deploy through restricted SSH; see [deployment setup](docs/deployment.md).

For browser smoke, start imported app, install Chromium once if needed, then:

```bash
dotnet run --project Tests/MonatroxPortfolio.BrowserTests -- --install
PORTFOLIO_BROWSER_URL=http://127.0.0.1:5420 bash scripts/verify.sh
```

Or run just browser smoke:

```bash
dotnet run --project Tests/MonatroxPortfolio.BrowserTests -- http://127.0.0.1:5420
```

Browser check expects supplied portfolio content and imported CCF/Distill/Portfolio pages. Covers no-JS HTML, WASM runtime, no startup API refetch/Server circuits, professional/gamer palettes, permanent avatar-bubble capsule in both modes with original white/lime logo, contrasting header text, fingerprinted MX favicon, single-row numbered mobile navigation with narrow-screen name hiding, no preview controls and mobile overflow checks, person-focused labels without mode/slogan clutter, oversized stacked selectors/opposed icons, professional portrait/gamer logo swap, mode-specific About portraits, aligned About/supporting cards, LinkedIn/GitHub targets, Discord copy, local-capture-only email form with validation/focus return/light-dark/mobile layouts and draft-preserving 503/429 handling, stable selector colors during blocked WASM startup/after handoff/without JS, identity-first landing screen with below-fold biography/projects, state-preserving keyboard view switching, no heading autofocus, skip-link/top-link focus, skills/background toggles, keyboard dialog/focus return, exactly three real project-grid entries without demo cards, scrollable case-study dialogs with unchanged portfolio URL/title, focus/scroll restoration, native Ctrl-click/direct-page access, full no-JS dedicated-page content and no handoff refetch, padded keyboard workflow tabs with colored active indicators, intact inline Markdown, code copy, native disclosures, offline calculator controls, missing-page/503 recovery and 320px–1366px page overflow checks, 1920×1080/1366×768 landing-screen fit, 1920px/1366px/1024px card alignment and basic 320px/390px/768px overflow checks, reduced motion, offline local interactions and same-origin 404/503 recovery through Retry. Review screenshots are written to `/tmp/monatrox-professional-design/` on Linux. On Nobara, Playwright uses its Ubuntu fallback build. No official Playwright OS-support, full accessibility/responsive audit, screenshot-perfect or production acceptance claim.

## Architecture

- `Modules/Profile`, `Modules/Projects` and `Modules/Contact`: Application, Domain, Infrastructure, Public, Tests. Contact owns validated public email submissions/SMTP, with no message database or EF migration.
- Endpoints/use cases/validation/Public implementations belong to **Application**; EF mappings, contexts, migrations and persistence implementations belong to **Infrastructure**.
- Cross-module access uses only provider **Public** projects. Domain/Public remain framework-free. No foreign table access or shared business entity library.
- API host composes modules. `Hosts/MonatroxPortfolio.Blazor` owns HTML shell, prerender HTTP registration and transport-only YARP gateway; it references client project and shared service defaults. `Hosts/MonatroxPortfolio.Blazor.Client` owns UI/typed HTTP client and references only module Public contracts. Client contains no server-framework/service-default/database dependencies. Frontend never accesses backend implementations.
- Profile and Projects have their own prefixed tables and migration history (`__ProfileMigrations`, `__ProjectsMigrations`) in one file; Contact has no database. API/frontend startup never applies migrations.
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

SDK/package/image pins are versioned. SDK 10.0.114 and runtime 10.0.14 support patched Blazor WASM packages/browser runtime 10.0.14 and Release linker. Frontend host requires runtime 10.0.14; EF remains pinned to 10.0.9, MudBlazor to 9.11.0, Markdig to 1.6.0, MailKit to 4.18.2, Aspire to 13.6.0 and YARP to 2.3.0. Explicit patched native SQLite and MessagePack dependencies address upstream transitive advisories; warnings are not suppressed. Review/update pins and audit dependencies before deployment. Identity-first design, production hosting, live contact email and CI/CD are in place. Further visual refinement, authentication/admin and additional operational hardening remain separate work; automatic deployment is not a claim of zero downtime or comprehensive production-readiness review. Contact limits/privacy and server-only SMTP configuration are documented in [docs/contact.md](docs/contact.md).
