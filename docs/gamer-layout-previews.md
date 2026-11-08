# Gamer layout previews

Branch: `gamer-layout-previews`, based on the local `skills-refresh` checkpoint `e2faae5`. No push, merge or deployment is part of this preview task.

## Try them

Open **http://127.0.0.1:5422**, choose **Monatrox**, then use the fixed selector at the bottom of the screen:

| Concept | Direction |
| --- | --- |
| Clawmark | Obsidian/crimson, gritty textures, background claw slashes and torn framing. |
| Neon | Violet/cyan cyberpunk, glass panels, architectural light lines and asymmetric composition. |
| Arcade | Brutalist cream/orange poster, hard shadows, chunky typography and checkerboard trim. |
| Manuscript | Dark fantasy codex, antique gold, serif display type and engraved ornament. |
| Tactical | Graphite/olive command HUD, amber accents, grid surfaces and dossier cards. |
| CRT | Green phosphor terminal, static scanlines, monospace type and terminal-like project rows. |

Six separate Pi CLI subagents used **openai-codex / gpt-6.1-sol**, **high** thinking. Each designed its own CSS fragment with read/write-only tools, no worktrees and no shared-file edits. Parent integration combines the styles in `wwwroot/gamer-previews.css` and owns the selector and checks.

Selection is in-memory only, retained while switching identities. Reload starts in the existing professional view; switching to gamer initially selects Clawmark. Selecting a concept does not change content, refetch profile data, or navigate away.

## Isolation and assets

Concept rules require both `data-view="gamer"` and the selected `data-gamer-preview` on the shell. Root palette changes use the same guard. The bottom picker is not rendered in professional mode. Dialogs remain above it, and gamer-only bottom padding keeps the footer accessible.

Original photographs and white/lime raster logos are unchanged. No remote fonts, new packages, invented content, telemetry claims or message submissions. Effects are CSS-only and static; base reduced-motion duration safeguards retain MudBlazor completion events.

The skills grouping introduced on `skills-refresh` is shared intentionally. The gamer concepts themselves do not alter professional layout or theme.

## Local checks

Build the existing Playwright runner, then run the focused checks:

```sh
dotnet build Tests/MonatroxPortfolio.BrowserTests
node Tests/MonatroxPortfolio.BrowserTests/gamer-previews.cjs http://127.0.0.1:5422/
```

The Node script reuses Playwright shipped by the existing NuGet dependency; no npm install. It accepts loopback URLs only and blocks non-GET/HEAD requests. It checks:

- All six selectable concepts, keyboard activation and pressed states.
- Desktop, tablet, 421px, 390px and 320px layouts, including a wider system-font fallback, header containment and 44px-high picker targets.
- All 44 retained skills and nine section icons.
- Case-study modal opening/closing in each concept.
- No preview CSS rule matches professional elements, and professional screenshots are identical after every concept switch at 1366px and 390px. Only transient button ripple paint is excluded from screenshot comparisons.
- Browser exceptions and failed resource responses.

Screenshots are written under ignored `artifacts/gamer-previews/`.

The earlier skills pass passed 109 .NET tests, content validation and unchanged EF-model checks. Its broad browser runs encountered cold-start/header timeouts; a full-run retry after integration also hit a WebAssembly handoff timeout. Those runs are not reported as passing. The focused six-concept checks pass, as do the final frontend integration tests. Focused preview checks do not replace a complete accessibility, real-device or production-readiness audit.

The preview uses a separate local SQLite database and API on port 5412, not production or the existing Aspire database. Final skill content and the final gamer design still await Zac's selection.
