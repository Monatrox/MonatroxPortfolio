# MonatroxPortfolio v1 — working plan

## Agreed scope

- Rebuild BoringPortfolio as a strict modular monolith. Start from the new repository, not the old solution.
- Temporary Blazor Server frontend: near-copy old dark MudBlazor layout, logos and components. Keep unfinished portfolio placeholders. Remove demos, Identity templates and debug actions. WASM and substantial redesign are later work.
- Public, read-only site. No admin, authentication, visitor accounts or HTTP write endpoints.
- **Profile** owns biography, social links, skills, employment and education. **Projects** owns project listings/details. No empty Contact/Auth modules.
- Reuse old source content and five dummy projects, not old databases. Keep content in versioned JSON files; explicitly import stable IDs, repeat-safely, without deleting unlisted records or resetting data on startup.
- EF Core + one SQLite file. Each module owns contexts, prefixed tables, independent migration history and migrations.
- Aspire orchestrates hosts using Podman; smtp4dev supplies local SMTP and inbox UI only. Loopback-only development. No RabbitMQ, Wolverine, cache, SignalR, tunnels or production deployment.
- Local verification: architecture, module behavior, real SQLite migration/import and API tests, plus focused homepage browser smoke. No GitHub Actions yet.

## Boundaries

Physical directories mirror solution folders. Names use `MonatroxPortfolio.{Module}.{Layer}`. `.slnx` is the only solution file.

| Location/layer | Responsibility | Dependencies |
| --- | --- | --- |
| `Hosts/` | API, BlazorServer, AppHost, Migrations; composition/transport only | Explicit wiring exceptions, no host business logic |
| Module Public | Provider-owned interfaces and DTOs | Technical contracts; only justified acyclic foreign Public edges |
| Module Domain | Owned entities, value objects and invariants | Framework-free; no foreign modules or own Public/Application/Infrastructure |
| Module Application | Use cases, validation, minimal API endpoints, implementations of Public capabilities, persistence ports | Own Domain/Public, technical API/contracts, foreign Public only |
| Module Infrastructure | EF contexts, mappings, migrations, hydration/persistence and external adapters | Own Application/Domain/Public, technical infrastructure, foreign Public only |
| Module Tests | Owned behavior and persistence verification | Own layers; foreign modules through Public only |
| `Shared/` | Technical contracts/helpers only | Never shared business entities |
| `Tests/` | Cross-cutting architecture/integration/browser checks | Explicit test-only implementation access |

No foreign tables, joins, navigation properties, cache inspection or reflection bypasses. C# public accessibility is not an integration contract. Domain/Public expose no EF, HTTP or implementation types. No mandatory generic repository/mediator. Direct calls through Public are fine; no artificial inter-module interactions.

Frontend owns all UI and typed HTTP clients. It accesses backend only over HTTP and references no backend implementation or database packages. API maps Application-owned endpoints and registers Infrastructure. Expected operation failures are transport-neutral; HTTP adapters map them. Unexpected failures remain exceptions. One operation has one commit owner; no implied cross-module transaction.

## Baseline and execution

- .NET 10 throughout; pinned SDK, central package versions, nullable enabled and consistent builds.
- Hosts: `MonatroxPortfolio.BlazorServer`, `.Api`, `.AppHost`, `.Migrations`. Shared health/telemetry/discovery helpers live in `Shared/MonatroxPortfolio.ServiceDefaults`.
- Minimal API endpoints live in Application projects, not Infrastructure or API host.
- Dedicated migrations host runs before API under Aspire. Content import is a separate explicit command. Module-owned contributors perform migration/import work; host only coordinates.
- Local data, IDE settings, build outputs and credentials stay out of Git. Production storage/email/deployment remain undecided.
- Import validates inputs before writes. Each module commits its own import; a later module/storage failure may require rerunning the repeat-safe command. No cross-module atomicity claim.

## Implementation checklist

- [x] Agree v1 decisions; establish Git baseline.
- [x] Record this working plan.
- [x] Restructure solution and establish dependency boundaries.
- [x] Implement Profile and Projects contracts/domain/application/persistence.
- [x] Generate separate EF migrations and explicit JSON import tooling.
- [x] Adapt old frontend to HTTP contracts; preserve visual placeholders.
- [x] Wire Aspire/Podman/smtp4dev and loopback endpoints.
- [x] Add local verification and run focused automated checks.
- [x] Document commands, actual evidence and remaining limits.

## Validation log

Implemented and checked locally on SDK 10.0.109 / runtime 10.0.9, Podman 5.8.4. Commands live in root README and `scripts/`.

- Debug and Release solutions build with zero warnings/errors; central package pins and local EF tool restored.
- 43 passing tests: 14 architecture checks/negative fixtures, seven Profile, 19 Projects, three integration/API/CLI. Tests use temporary SQLite files.
- Both EF models report no pending migration changes. Independent migrations coexist in one real file and are repeat-safe.
- Backend review corrections: Profile collections now expose read-only views with guarded domain add methods; Application mapping and Infrastructure merge use those methods. EF hydrates backing fields without schema changes. Tests cover direct mutation rejection, duplicate/null additions and repeat imports adding all four child types while preserving unlisted rows.
- Moved service-default helpers from Hosts to Shared; updated project/solution references and added placement regression check.
- Project URL validation now accepts root-relative web paths while continuing to reject network-path references, backslashes, control characters and unsafe schemes. Regression tests cover link and thumbnail fields.
- Latest backend/API/Blazor build was exercised against existing development SQLite in read-only mode on isolated temporary ports, including Chromium smoke. Temporary test hosts stopped afterward; existing Aspire preview and disposable smtp4dev inbox were left untouched.
- Import tests cover repeat imports, stable-ID updates, preserving unlisted records, rejection of invalid/duplicate/null content, and validating both module documents before writes.
- Chromium smoke passes against actual Aspire-hosted frontend/API: homepage, logo, skills/background expand/collapse, keyboard dialog/escape, placeholders and carousel navigation. Buttons stay disabled during prerender until handlers are live; browser testing caught and verified this fix.
- Removed template `FocusOnNavigate` heading autofocus from temporary single-page frontend after visual review. Browser regression reproduced initial heading focus before removal and passes afterward. Keyboard focus indicators and dialog/card keyboard behavior remain intact.
- Aspire starts migrations, API, Blazor Server and actual Podman smtp4dev 3.16.1. API/frontend/inbox return HTTP 200. Process listeners and container port mappings were inspected: loopback only.
- Synthetic SMTP message sent through Aspire-provided SMTP address/port appears in smtp4dev inbox API. This verifies wiring, not a Contact Me implementation.
- Initial source content imported explicitly into ignored development database. Routine Aspire restart does not reseed.
- Pre-baseline secret review: checksum-verified Gitleaks 8.30.1 scanned exact staged snapshot and existing Git history with no leaks detected. Manual review covered credential-related source/configuration, image metadata chunks, and staged-file exclusions. `UserSecretsId` is an identifier, not a credential. Local SQLite files, IDE state, build output and common credential files remain ignored. Scanning cannot guarantee detection of every possible secret.
- Restore exposed upstream native SQLite/MessagePack advisories. Explicit patched bundles (SQLitePCLRaw.bundle_e_sqlite3 3.0.5 and MessagePack 2.5.303) resolve them without suppressing warnings. Full solution transitive NuGet audit reports no known vulnerable packages against current advisory sources.

Limits: no mobile/visual-regression acceptance, full accessibility audit, production deployment/security readiness, WASM, admin/auth, contact behavior or global import transaction. Architecture tests enforce references/source conventions but do not replace semantic review of SQL/business integration. smtp4dev data is disposable. Browser smoke is explicit and expects initial sample content. Playwright reports Nobara as unsupported and uses its Ubuntu fallback build; local Chromium smoke passed, but this is not an official OS-support guarantee. No new user decisions were required beyond the agreed scope.
