# Skills refresh

Working branch: `skill-group-updates`, based on `develop`. Local review only; no production changes or main merge.

## Approved structure and wording

Nine groups, with .NET tools first in their relevant categories rather than a dedicated .NET section:

1. Languages & Scripting
2. Web Development & APIs
3. Architecture & System Design
4. Data & Messaging
5. Testing & Diagnostics
6. Developer Tooling & Automation
7. AI Tooling & Agent Workflows
8. Hosting, Deployment & Orchestration
9. Project Management & Technical Leadership

The section introduction reads:

> A selection of technologies and practices from my experience—not an exhaustive inventory.

People/project leadership stays in the final group. Technical legacy-system analysis and incremental modernization remain under Architecture. Delivery prioritization is expressed as **Balancing feature delivery, legacy maintenance & technical debt**.

## Evidence and confirmed additions

- CCF supports ASP.NET Core MVC, MySQL, stored procedures, requirements discovery, stakeholder demos, constrained delivery, training and support handover.
- Distill supports Rust, .NET integrations, RabbitMQ, actor-based processing, bounded queues, idempotency, recoverable workflows, storage adapters and failure/contract testing. Its documented local results do not establish production throughput or validated Azure deployment compatibility.
- This portfolio demonstrates prerendered Blazor WASM, modular ownership, EF Core/SQLite migrations, YARP, xUnit/Playwright, OpenTelemetry, Aspire, Docker/Podman, CI/CD, Nginx/TLS and deployment rollback automation.
- Zac explicitly confirmed IIS administration, Azure deployments, multiple MCP integrations, custom AI skill files/plugins/integrations, deep Dapper/SignalR/Valkey experience and a career balancing feature delivery with legacy maintenance.
- SQL Server, PostgreSQL, MySQL and SQLite are listed as database products worked with, not narrowly as project-specific adapters.
- AI tools retain the previously supplied inventory: Pi, Codex, Claude Code, OpenCode, Copilot, Cursor, Warp, LM Studio and Ollama. Custom integrations do not imply RAG, model training or a particular AI application SDK.

No new claims of Roslyn/source-generator development, gRPC, BenchmarkDotNet, Kubernetes, RAG or AI evaluations are added. Specific Azure services are not guessed.

## Follow-up refinements

- Language order follows Zac's experience: **C#, C++, Python, SQL, JavaScript, TypeScript, Bash, Rust**.
- **Engineering standards & technical reviews** replaces the basic review/PR entry and moves into leadership, retaining its original stable ID.
- Testing now includes mocking/test doubles, in-process application/API integration, Testcontainers, architectural fitness tests, contract/compatibility tests, A/B testing and canary validation alongside failure recovery and browser checks.
- **Feature flags & dark deployments** belongs under hosting/deployment.
- These testing/release additions are user-supplied experience, not claims that this portfolio uses Testcontainers, runs A/B experiments or implements feature flags/canary/dark deployments. No extra packages or runtime release mechanisms are added.

## Data and compatibility

`content/profile.json` now contains 81 skill records across nine groups, rendered as 77 list entries. All 44 previous stable skill IDs are retained and recategorized or renamed where appropriate; 37 entries are added. Original IDE/API/database tools, game engines and Power Platform remain under developer tooling, and SharePoint under web/integration work. They are not silently retired or hidden. Three tool families are combined only in presentation: Postman/Insomnia/Bruno; Rider/DataGrip/other JetBrains tools; Unreal Engine/Unity. The renderer joins the names actually present, preserving partial families and unrelated custom tools. APIs/imports retain individual records, so there is no destructive retirement or duplicate resurrection on an existing database.

The category string/DTO/storage shape is unchanged. `Web`, `Diagnostics` and `Leadership` join the validated category set. Older category values remain accepted/renderable for preserved custom content. No EF migration is needed.

Imports still upsert explicit IDs and **preserve unlisted records**. A regression test imports representative old-category records plus an unlisted custom skill, refreshes twice, and verifies category/name updates without deleting custom data. No production or Aspire database is imported for this preview.

If later reducing/consolidating existing records, identify explicit superseded IDs and approve a backed-up retirement step. Omission from JSON must not become a global delete policy.

## Review checks

- Domain/import/API/prerender checks cover the new categories, confirmed content and nine-group count.
- Browser checks cover section-only icons, all 77 displayed entries, exact language ordering, combined tool labels, advanced testing/release wording, disclaimer, IIS/custom AI content and responsive containment in both identities.
- Existing Neon Flatline visuals, navigation and case-study behavior remain unchanged.
- Preview uses loopback-only API/frontend, an isolated SQLite copy and disabled email. Logs/state/published output live under ignored `artifacts/skills-refresh-lab/`.

Further list consolidation or wording changes should be reviewed before publication. Stored-record consolidation would still need explicit retirement approval; the current display-only grouping avoids that data change.
