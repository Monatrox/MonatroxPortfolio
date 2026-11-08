# Skills refresh — content proposal

Working branch: `skills-refresh`. Structure draft only; production remains unchanged and the final content list is pending.

## Structure pass

The preview uses nine icon-headed groups: .NET & APIs; Languages; Architecture & leadership; Data & messaging; Containers & delivery; Developer workflow & quality; Business platforms; AI-assisted development; Game development. .NET leads the layout. Introductory copy highlights the case studies' source generation, structured logging, Rust services and distributed application orchestration.

All 44 original skills and their stable IDs are retained; only categories change. Database products now belong under Data. No proposed candidate tools are added yet and no superseded records are deleted. Legacy Tools / Skills categories remain valid and render as Other tools / Other skills, so preserved unlisted content is not silently hidden.

The existing category field and public DTO shape are reused: no EF schema change or new table. The remaining sections are a candidate inventory for the final content pass, not the exact initial preview contents.

**Baseline** means already listed in the profile, explicitly named by Zac in this request, or demonstrated by this repository where noted. **Candidates** are questions for Zac, not claims of experience. Familiarity with one product does not imply experience with its whole ecosystem or operating it at enterprise scale.

## Proposed presentation

Replace the three broad Tools / Skills / Languages lists with nine compact, headed lists. Group interchangeable utilities into one line; keep meaningful engineering capabilities visible. Use ordinary text, not logos, proficiency scores, or an expanding catalogue of keywords.

Order engineering and delivery before supporting utilities. Keep game development last on the professional view rather than deleting that experience.

### 1. Architecture & technical delivery

Baseline:
- Software design & microservice architecture.
- Legacy-system investigation & debugging.
- Project planning, delivery & client communication.
- Team leadership & development.

Candidate capabilities to confirm:
- RFCs / ADRs, design reviews and cross-team technical alignment.
- Incremental modernization, service boundaries and migration planning.
- Incident leadership, capacity planning, reliability / cost trade-offs.

These describe staff-relevant work without claiming a staff-engineer title or inventing impact metrics.

### 2. Languages & application frameworks

Baseline:
- C#, C++, Python, JavaScript, TypeScript.
- ASP.NET / ASP.NET Core, Blazor, SignalR.
- Entity Framework / EF Core, Dapper.
- API design & development.

Candidates: Rust, Go, Bash, PowerShell; gRPC, OpenAPI / Swagger, YARP; React or another frontend framework only if useful to represent actual work.

ASP.NET Core and EF Core are demonstrated here; the existing profile's broader ASP.NET / Entity Framework experience should not silently disappear.

### 3. Data & messaging

Baseline:
- Relational databases: SQL Server, PostgreSQL, MySQL, SQLite.
- Messaging & caching: RabbitMQ, Redis.
- SQL Server Management Studio.

Candidates: query tuning, indexing, transactions and schema migrations; Azure Service Bus or Kafka; Elasticsearch / OpenSearch; pgvector or another vector store.

Database products move out of “Languages.” SQL can be listed as a language if Zac confirms that wording. Avoid implying Redis or RabbitMQ automatically means expertise in every distributed-systems pattern.

### 4. Containers, delivery & operations

Baseline:
- Containers & orchestration: Docker, Podman, Kubernetes, .NET Aspire.
- Git, Azure DevOps; GitHub Actions and container delivery demonstrated by this portfolio.

Candidates:
- Cloud / infrastructure: Azure, AWS, Terraform, Helm, Linux, Nginx.
- Observability: OpenTelemetry, Grafana, Prometheus, Application Insights.
- Reliability: retries / idempotency, backpressure, SLOs, production incident response.

Docker / Podman are container tools, Kubernetes is a cluster orchestrator, and Aspire orchestrates distributed application development. Grouping them is useful; treating them as equivalent experience is not.

### 5. Developer workflow & quality

Baseline:
- Various IDEs (VS, VS Code, JetBrains IDEs, Cursor).
- API clients (Postman, Insomnia, Bruno).
- Playwright; test automation, testing / QA and code review.
- Jira.

Candidates: xUnit / NUnit, Testcontainers, BenchmarkDotNet, k6 / JMeter, contract testing, accessibility testing.

Rider and DataGrip are represented by “JetBrains IDEs,” rather than repeated as separate badges. Keep specific products in project stories where their use matters.

### 6. Business platforms & automation

Baseline:
- SharePoint, Microsoft Power Platform, Microsoft Office.

Candidates: Power Apps, Power Automate, Power BI, Dataverse, Microsoft Graph, Teams integrations, Excel / Power Query, business-process automation and enterprise-system integration.

Confirm individual Power Platform products; the umbrella name does not establish experience with all of them. Prefer concrete workflow / integration work over generic “business skills.”

### 7. AI-assisted development

Baseline:
- Coding agents / assistants: Pi, OpenCode, Claude Code, Codex, Warp, GitHub Copilot.
- Cursor is already covered under IDEs; do not repeat it just to lengthen this list.

Candidate capabilities: repository context and agent instructions, tool / MCP integration, agent-assisted debugging and testing, reviewing generated changes, managing context / cost / permissions.

Keep use of coding assistants separate from building AI applications or training models.

### 8. Local AI & application integration

Baseline:
- Local model tools: LM Studio, Ollama.

Candidates:
- Runners / serving: llama.cpp, vLLM, Open WebUI.
- Application SDKs: Microsoft.Extensions.AI, Semantic Kernel, Microsoft Agent Framework.
- Capabilities: model / provider integration, structured outputs, tool calling, retrieval / RAG, evaluations, embeddings, quantization and inference resource trade-offs.

Open WebUI is an interface, not an inference engine. Only list the runners and AI application capabilities actually used; do not infer them from having a local model installed.

### 9. Game development

Baseline: Unreal Engine, Unity.

Keep concise. No need to expand this category for a business / distributed-systems / AI-focused professional presentation.

## Data and UI recommendation

The current model already has stable skill IDs, a category string and sort order. The domain restricts categories to three values, and `Home.razor` hardcodes those three lists.

**Recommended starting point:** reuse that model, approve a finite category set, and render compact grouped lists. New tables or nested public DTOs are not automatically necessary for this presentation. Confirm the group layout before changing domain validation or API semantics.

Consolidation also needs an explicit retirement plan: the importer deliberately preserves omitted records. Removing VS Code / Rider / DataGrip / Insomnia rows from JSON alone will not remove their stored rows. Do not change that global import policy. Any targeted cleanup must identify superseded stable IDs, preserve unrelated content and be backed up before application to an existing database.

Implementation scope after approval:
- Profile content, domain category validation and any approved explicit retirement mechanism.
- Profile-owned import / persistence behavior and its preserve-unlisted regression tests.
- Shared homepage renderer and responsive list styling in both identities.
- Prerender / API / browser fixtures that currently assume 44 skills, three headings or individual IDE labels.
- Local backed-up import and preview; no production change, push or merge without approval.

## Review checklist

1. Confirm group names and the baseline wording.
2. Mark candidate tools / capabilities actually used; leave the rest out.
3. Flag exposure versus substantial hands-on experience where a plain list would mislead.
4. Approve implementation and explicit handling of superseded rows.

## Product references for candidate classification

These clarify what the tools do; they are not evidence of Zac's experience:
- [.NET AI ecosystem](https://learn.microsoft.com/en-us/dotnet/ai/dotnet-ai-ecosystem)
- [OpenTelemetry overview](https://opentelemetry.io/docs/what-is-opentelemetry/)
- [vLLM serving](https://docs.vllm.ai/en/stable/serving/openai_compatible_server/)
