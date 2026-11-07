# WASM migration — working plan

## Checkpoint

- Baseline: `d880b16` on `main`, pushed after secret review.
- Work branch: `wasm-migration`, tracking origin. Keep this milestone separate from visual redesign.

## Agreed scope

- Preserve current dark MudBlazor UI, content, logo assets and intentional placeholders.
- Replace Interactive Server with **Interactive WebAssembly plus initial HTML prerendering**. No Interactive Auto fallback or Blazor Server circuits.
- Thin frontend host: `Hosts/MonatroxPortfolio.Blazor`. Browser UI/bootstrap: `Hosts/MonatroxPortfolio.Blazor.Client`. Both remain host-owned, not module UI projects.
- Backend modules, Public contracts, migrations and SQLite layout stay unchanged.
- Same-origin `/api` gateway in frontend host using YARP direct forwarding. Browser only knows frontend origin; module Application projects retain business endpoints. Prerendering accesses existing API through configured HTTP client.
- Persist public prerendered profile/projects/error state into browser startup. No secrets or server configuration in persisted state; avoid duplicate API loads or loading flicker during handoff.
- Keep controls disabled until interactive. Preserve keyboard/dialog behavior and no heading autofocus.
- API outage must not destroy local UI interaction; retry remains available. HTTP operations still require available API.
- Adapt Aspire wiring, architecture checks, browser checks and local commands to new hosts. Retire temporary BlazorServer project once parity is demonstrated; Git baseline preserves original.

## Checks

- [x] Browser-only client references only module Public contracts; no server framework/service-default/database dependencies.
- [x] Host references client and technical service defaults; no backend implementations.
- [x] Initial HTML includes portfolio content with JavaScript disabled.
- [x] Browser becomes WebAssembly-interactive, not Server/Auto; no `/_blazor` requests or WebSockets.
- [x] State handoff does not refetch profile/projects; no duplicate loading/render flash.
- [x] Same-origin gateway preserves API responses/errors; upstream is configured, never chosen by request.
- [x] Existing homepage interactions and no-autofocus regression pass.
- [x] Error and retry behavior, missing content and API-unavailable cases checked.
- [x] Debug/Release build, real published Release browser smoke, backend/architecture tests and EF model checks pass.
- [x] Aspire + Podman + smtp4dev wiring and local/publish commands documented.

## Later design direction — not this milestone

Start with professional presentation: light theme and polished modern motion. Later, diagonal split entry between **Zac Taylor** (professional) and **Monatrox** (gamer), animating selected identity full-screen. Gamer view gets dark artistic styling/effects. Broad layout shared; typography, colors, containers, motion and selected sections vary. Keyboard access and reduced-motion behavior are requirements when that work begins.

## Evidence / limits

- UI moved from BlazorServer into Blazor.Client; host owns shell, static assets and transport-only YARP gateway. Temporary BlazorServer project retired; baseline remains on main.
- .NET 10 `[PersistentState]` transfers public Profile/Projects/Error properties across prerendering. `RendererInfo.IsInteractive` replaces manual interactive-state bookkeeping. Razor analyzer rejects persistent-property initializers, so state starts nullable and loads only when no restored state/error is present.
- Same-origin gateway uses one validated HTTP(S) origin; rejects credentials/path/query/fragment in upstream configuration. No request-selected destination, automatic redirect following or custom handwritten proxy protocol.
- Debug and Release solutions build with zero warnings/errors. All **54 tests** pass in both configurations: 15 architecture checks, seven Profile, 19 Projects, 13 integration/API/CLI/frontend checks. Backend modules, Public contracts, Shared code, content and schema source remain unchanged. API host gained namespaced assembly marker for unambiguous integration-test entry point.
- Both EF model checks report no pending changes. Development SQLite retains 44 skills/five sample projects; no import/reset performed by migration.
- Chromium smoke passes against Debug host, actual Aspire-hosted frontend, and real trimmed published Release output in Production mode. Covers no-JS HTML, WASM renderer, no initial browser API refetch/circuit requests, no heading autofocus, keyboard dialog/escape, carousel, collapsed sections, local interaction while offline, and client-side 404/503 failures followed by successful same-origin Retry.
- New frontend integration tests verify one profile/project fetch during prerender, persisted handoff state, no upstream address in HTML, gateway path/query/status/Problem Details preservation, no redirect following, missing content, unavailable upstream and invalid origin configuration.
- Release IL trimming remains enabled; Public DTO assemblies explicitly retain JSON reflection metadata. AOT is not enabled. No linker-warning suppression or blanket trimming disablement.
- Older WASM debugger package had known advisories. WASM packages/browser runtime and frontend server minimum now pin **10.0.14**. Release linker required same patched runtime: user approved checksum-verified official SDK **10.0.114** installation into `~/.dotnet`. Repository SDK pin updated within same 10.0.1xx feature band. System packages/permanent shell configuration unchanged; README documents terminal PATH and Rider CLI selection.
- Full transitive NuGet audit reports no known vulnerable packages against current sources. Gitleaks 8.30.1 scan of current tracked/untracked source snapshot found no leaks; generated output, local databases and runtime environment remain excluded.
- Aspire now launches new Blazor host with Podman smtp4dev and unchanged migration/API setup. Preview runs at `http://127.0.0.1:5420`; gateway at `/api`. Inspected process/container listeners remain loopback-only. Disposable smtp4dev inbox reset during development-stack restart; synthetic message through Aspire-provided SMTP settings verified delivery afterward.

Limits: no deployment, authentication/admin, Contact Me behavior, offline-write, AOT or numerical performance guarantees added by this migration. Browser smoke expects sample content and uses Playwright's Ubuntu fallback on unsupported Nobara; no mobile/visual-regression/full accessibility acceptance. Browser telemetry is not automatically supplied by server service defaults. Activate user-local SDK for future CLI/Rider builds; production frontend requires patched runtime 10.0.14.
