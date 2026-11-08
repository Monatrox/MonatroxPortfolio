# Project pages

## Agreed design

Project pages live in **Projects**, not a CMS module. Author one validated JSON file per project, using Markdown for prose and reusable blocks for layout. SQLite stores one optional versioned document row per project; homepage metadata stays separate. These choices were explicitly approved.

Reference drafts were **copied**, byte-for-byte, into ignored `docs/projects/CCF.html` and `docs/projects/Distill.html`. Originals remain untouched. These HTML files are review material, never runtime input. Converted, versioned content lives in `content/project-pages/`.

Both pages preserve oversized title, two-column opening/visual, tags, three-part overview, numbered project-specific sections, navy panels, green accents, cards, diagrams, tables and code. Normal homepage card clicks open wide, internally scrollable dialogs, without document/site navigation or footer inside them. Shared `ProjectDetails` renders the same content in dialogs and at dedicated URLs; direct/new-tab pages retain shared portfolio chrome. Ctrl/Cmd-click, middle-click and browser open-in-new-tab continue to use real project links; dialogs also offer “Open full page.” Escape/close returns focus to the card without replacing the portfolio route/state. Substantive footer sources, revision/date and qualifications remain document notes. Responsive layouts stack; wide tables/code scroll within their containers.

CCF remains a client/workflow story, Distill an engineering guide. No mandatory Problem/Solution/Results chapters or invented achievements. Their text and evidence are author-supplied drafts, **not independently verified claims or production-publication approval**.

## Add a page

From repository root, with local SDK activated:

```bash
cp content/project-page.template.json content/project-pages/your-project.json
# Edit the new file.
bash scripts/dev.sh validate  # Validate every import document; opens no database.
bash scripts/dev.sh import    # Explicit migration + stable-ID upsert into local SQLite.
bash scripts/dev.sh           # Start preview if not already running.
```

1. Set stable `id`, `name`, listing `description`, existing/added `thumbnailSrc`, and `sortOrder`.
2. Set `link` to `/projects/<id>` so homepage card opens its case-study dialog, with that URL retained for direct/new-tab access. Source/repository links belong in page prose/notes.
3. Edit `page.hero`, overview facts and ordered sections. Section IDs must be unique ASCII letter/digit/hyphen anchors, at most 80 characters. Their order determines visible numbering.
4. Replace template placeholders. Remove irrelevant sections rather than filling them with invented claims. Describe role, scope, current state, evidence, tradeoffs and limitations honestly.
5. Preview `/projects/<id>` at desktop/mobile widths; test links, keyboard controls and any special blocks. No Razor changes are needed for ordinary pages.

Keep template and schema **outside** `content/project-pages/`; every top-level `*.json` there is discovered explicitly by import/validation tooling. Files are combined with legacy `content/projects.json` before Projects validation. Duplicate IDs across files fail. Published content contains CCF, Distill and Portfolio; `content/projects.json` is empty. Synthetic project records remain isolated under Projects.Tests/Fixtures for importer regression tests, not publication.

Existing IDs update; new IDs insert. Omitted records/pages are **not deleted**. A metadata-only legacy entry does not remove an existing page. Changing ID creates another project, not a rename. No startup imports, public writes, automatic file watching or admin editor. Module commits remain independent after all inputs validate; back up databases before operational changes.

## Editor assistance

Associate `content/project-page.schema.json` with `content/project-pages/*.json` and `content/project-page.template.json` in your editor's JSON Schema settings. Schema supplies field names, required structure and discriminated block choices; importer additionally enforces semantic limits/links/layouts. Do not add `$schema` to documents: unknown fields are rejected.

Regenerate schema after changing Public contracts:

```bash
dotnet run --no-build --project Hosts/MonatroxPortfolio.Migrations -- --project-schema > content/project-page.schema.json
```

Build first if contracts changed. Schema command needs no database. `page.schemaVersion` is currently **1**; unsupported versions fail rather than guessing a conversion.

## Content vocabulary

Every block has a `type`. Arrays retain authored order.

| Type | Required content | Optional layout/detail |
| --- | --- | --- |
| `markdown` | `markdown` | Paragraphs, lists, emphasis, links, inline code, pipe tables |
| `cards` | `cards`: `{ title, markdown }` | `columns` 1–3; card `eyebrow`, `footerMarkdown` |
| `columns` | `blocks` | `columns` 1–3; one child block per grid cell |
| `panel` | `blocks` | `title`, `tone`; `plain` groups without a frame |
| `table` | `caption`, `headers`, `rows` | Every row must match header count; cells are Markdown |
| `code` | `title`, `language`, `code` | `captionMarkdown`; language: `text`, `json`, `csharp`, `shell`, `rust` |
| `callout` | `markdown` | `title`, `tone` |
| `steps` | `steps`: `{ title, markdown }` | Step `eyebrow`, block `captionMarkdown`, `tone` |
| `diagram` | `nodes`: `{ title, markdown }` | `layout`: `chain` or `branch`; node `eyebrow`/`tone`; `captionMarkdown` |
| `tabs` | `tabs`: `{ title, blocks }` | `captionMarkdown`; all panels readable without JavaScript |
| `details` | `summary`, `blocks` | Native disclosure works without JavaScript |
| `calculator` | `assumptionsMarkdown` | Trusted Distill model, bounded defaults only—not formulas/scripts |

Tones: `plain`, `surface`, `accent`, `dark`, `note`, `warning`. Most styles need only default, `accent`, or `warning`; not every block has a visibly different treatment for every tone. In a branch diagram, first node spans above remaining nodes.

Page envelope includes hero/title/subtitle/intro/tags/visual, overview facts, optional introduction blocks, ordered sections and evidence notes. CCF uses introduction for prominent private-system qualification. See `ccf.json` for workflows/cards, `distill.json` for architecture/code/disclosures/calculator, and `portfolio.json` for a concise architecture demo using diagrams, layer responsibilities and the local development loop.

JSON strings use `\n\n` between Markdown paragraphs and `\n` inside code. Code is literal text, never executable document markup; copy button preserves its contents. HTML is disabled. Markdown links/images allow HTTP(S) and safe relative references, not script/file/data/network-path schemes or backslashes. Prefer root-relative site links and explicit HTTPS sources. Document source/review caveats and illustrative/not-screenshot qualifications rather than removing them with source chrome.

Validation bounds: canonical document 512,000 characters; individual text 100,000; at most 30 sections, 400 total blocks, 100 blocks per collection and six nested container levels. Tables allow 8 columns/100 rows; cards 30; diagram nodes 6; steps 12; tabs 8. Empty required text, null items, malformed table rows and unknown fields/types fail validation.

### Trusted calculator

Defaults: 40 instances × 25 events/instance/second × 60 seconds; error rate 0.001. Controls retain source bounds: instances 1–200, rate 1–500, window 10–300 seconds in steps of 10. Error rate configuration is bounded 0–1.

`events = instances × rate × window`; full errors are rounded up; retained records = errors + one aggregate; reduction = `(1 - retained / events) × 100`, displayed to two decimals. This is an **illustrative record-count model, not a benchmark**, throughput claim or production guarantee. No arbitrary stored formulas or JavaScript. Source assumptions remain visible beside results.

## Ownership and delivery

- **Public:** provider-owned summary/page DTOs and finite block contracts; existing summary endpoints unchanged.
- **Domain/Application:** document storage invariants, bounded content validation, repeat-safe import, read use cases/endpoints and persistence ports.
- **Infrastructure:** `Projects_ProjectPages` with restricted project FK; isolated Projects EF context/migration history. No per-block relational schema or foreign-module access.
- **Frontend:** homepage dialogs and `/projects/{id}` share `ProjectDetails`, safe Markdown renderer, reusable blocks, trusted interactions and scoped `project-pages.css`. Home intercepts ordinary SPA navigation to its listed case studies using Blazor's location-changing hook; native modified/new-tab clicks are not intercepted. Dialogs use existing MudBlazor focus trapping/restore, bounded content scrolling, close and Escape. Page/error state transfers from prerender to WASM without handoff API refetch. No Server/Auto circuits, raw HTML/script injection or iframe document embedding.
- **Distill content:** responsibility labels replace repository file paths; architecture/contract behavior is described in existing diagrams, examples and decisions. Setup includes an in-document sink manifest with environment/file secret references. Only the repository-root link remains at the bottom; revision/date and qualification limits remain visible.
- **Reads:** `GET /api/projects` and `/api/projects/{id}` still return lightweight summaries; `GET /api/projects/{id}/page` returns document or 404. Browser uses existing same-origin gateway.

Tests cover real-document roundtrips, metadata/page preservation, invalid content/unsafe links, all-input validation before writes, no-database validation, read-only additive API, full initial HTML and defensive Markdown rendering. Browser smoke exercises workflow selection, copy/disclosure/calculator, no-JS content, handoff, missing/error recovery and constrained widths alongside existing portfolio checks.

Local verification: **98 tests pass**, both EF models match migrations, Debug/Aspire and trimmed published Release browser smoke pass, and Gitleaks found no leaks in the working-tree source snapshot. CCF workflow panels have explicit padding and a colored active indicator; Distill's `ILogger` warning remains one inline sentence. Browser checks also cover modal focus/scroll restoration, fixed close control, 320px–1366px modal content bounds, unchanged portfolio URL/title and native Ctrl-click dedicated pages. Portfolio is a 633-word, intentionally overengineered demonstration of Aspire and a modular monolith. Its main examples are AppHost wiring/diagnostics, enforced module ownership and inspecting local mail; WASM rendering is supporting context. Three diagrams, a layer table and workflow commands show the running setup. Its modal/direct/no-JS views are included in smoke checks, including title wrapping and 320px–1366px content bounds. Openings were compared with references; screenshots live in `/tmp/monatrox-project-pages/`. This is not a full accessibility/device audit or pixel-perfect acceptance. Development SQLite was backed up before migration/import to `/tmp/monatrox-project-pages/portfolio-before-project-pages.db`; initial repeated import retained seven summaries and two document rows. After explicit user approval, the five unchanged demo records were removed from development SQLite following a fresh backup at `/tmp/monatrox-real-projects/portfolio-before-demo-removal.db`. No delete-by-omission, startup cleanup or generic deletion API was added. Other pre-existing databases still need deliberate cleanup; normal import preserves unlisted records.

Not included: CMS/admin/auth, drag-and-drop editor, arbitrary CSS/HTML/JS/formulas, automatic claim verification, deletion workflow, production publishing or new dependencies beyond Markdown parsing (Markdig).
