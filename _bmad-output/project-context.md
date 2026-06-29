---
project_name: 'Hexalith.Tenants'
user_name: 'Administrator'
date: '2026-06-02'
sections_completed:
  [
    'technology_stack',
    'language_rules',
    'domain_rules',
    'eventing_rules',
    'framework_rules',
    'identity_rules',
    'ui_rules',
    'testing_rules',
    'code_quality',
    'workflow_rules',
    'critical_rules',
  ]
status: 'complete'
rule_count: 61
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **.NET 10 / C#** — SDK pinned to `10.0.301` with `rollForward: latestPatch`; all owned projects target `net10.0`; `Nullable`, `ImplicitUsings`, `LangVersion=latest`, and `TreatWarningsAsErrors=true` are root defaults.
- **Solution/build** — `Hexalith.Tenants.slnx` only; `MSBuild.rsp` and `Directory.Solution.*` force single-node serialized builds (`-m:1`, `BuildInParallel=false`, `RestoreBuildInParallel=false`).
- **Hexalith platform dependencies** — `Hexalith.EventStore` packages pinned to `3.19.0`; `Hexalith.Memories` packages pinned to `1.31.1`. Debug uses source `ProjectReference` when available; Release uses NuGet packages for package-capable libraries.
- **DAPR** — DAPR SDK packages `1.18.4`; CI installs DAPR CLI/runtime `1.17.0`.
- **Aspire** — Aspire packages `13.4.6`; Keycloak/Kubernetes packages use `13.4.6-preview.1.26319.6`; DAPR hosting via `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview.1.260602-0230`.
- **Backend stack** — MediatR `14.1.0`, FluentValidation `12.1.1`, JWT/OpenID Connect IdentityModel `8.19.1`, OpenAPI `10.0.9`, Swagger UI `10.2.3`, OpenTelemetry `1.16.0` with Runtime instrumentation `1.15.1`.
- **UI stack** — Blazor InteractiveServer, FrontComposer Shell/Contracts source references, Fluent UI Blazor V5 `5.0.0-rc.3-26138.1`, bUnit `2.8.4-preview`.
- **Memories search** — Tenants UI uses `MemoriesClient.SearchAsync` as an index lookup only; rows are hydrated from Tenants REST query endpoints.
- **Testing** — xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`, Testcontainers `4.12.0`, coverlet `10.0.1`, Microsoft.NET.Test.Sdk `18.7.0`, YamlDotNet `18.0.0`.
- **Release tooling** — semantic-release `25.0.5`, commitlint `21.1.0`; five NuGet packages are released: `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, `.Aspire`.

## Critical Implementation Rules

### C# Language & Contract Rules

- Commands, success events, and rejection events are plain `public record` primary-constructor types. Do not add `sealed` or XML parameter docs to these contract records.
- Query contracts are `sealed class : IQueryContract` with static `QueryType`, `Domain`, and `ProjectionType`; `QueryType`/`Domain` are kebab-case and unique. There are currently 6 query contracts.
- Query response DTOs are `sealed record` types with XML `<summary>` docs; keep response DTOs separate from query contract marker classes.
- Events implement `IEventPayload`; rejection events implement `IRejectionEvent`; commands have no marker interface.
- Rejection records must stay structured and support-safe: no prose `Message`/`Reason`/`Detail`, no stack trace, no raw payload, no token fields.
- Every event type has a string `TenantId`; tenant/global administrator domain identifiers are meaningful caller-supplied strings, not GUIDs or ULIDs.
- Enums use `Unknown = 0` plus JSON string serialization. `TenantStatus` uses the custom converter that maps unrecognized values to `Unknown`; `TenantRole.Unknown` is non-privileged and rejected by domain logic.
- Use file-scoped namespaces, namespace = folder path, `using` outside namespace, System directives first, Allman braces, `_camelCase` private fields, `I` interfaces, and `Async` suffix on async methods.
- Always use `ConfigureAwait(false)` on awaited calls in production code. `CA2007` is warning-level in `.editorconfig`, but warnings are build failures.
- Keep each `.cs` file focused on one C# type/object. Move extra records/classes/enums/interfaces/delegates to their own files named for the type.
- Validate public boundaries with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`; avoid nullable suppression as a substitute for validation.
- Do not add copyright/license headers to new files in this repo.

### Domain, Eventing & Framework Rules

- Aggregates are pure domain functions: static `Handle(command, state?, envelope) -> DomainResult` and state `Apply(event)`. No I/O, no async, no mutation inside `Handle`; state changes only through `Apply`.
- Business failures are rejection events, not exceptions. Same-state domain requests return `DomainResult.NoOp()` only where explicitly modeled, such as same-role change or identical configuration set.
- The two aggregate domains are `tenants` and `global-administrators`. `TenantAggregate` uses `[EventStoreDomain("tenants")]`; global administrator events publish on the shared `tenants.events` topic via AppHost gateway topic override.
- Never edit/delete/rewrite events, projections, or state-store data to fix business state. Use compensating commands through `POST /api/v1/commands`, then verify command status and projection evidence.
- The Tenants host is an EventStore domain service, not an EventStore server. Do not call `AddEventStoreServer`, host `AggregateActor`, or reintroduce `TenantsProjectionActor`.
- Host composition consumes shared platform services: `AddServiceDefaults`, `AddEventStoreDomainTelemetry("tenants")`, `AddEventStoreDataProtection`, `AddEventStoreReadModelStore`, `AddEventStoreQueryCursorCodec`, and `MapEventStoreDomainService`.
- Tenant reads are served by in-process `IDomainQueryHandler`s and the REST query controller (`GET /api/tenants*`, `GET /api/users/{id}/tenants`, `GET /api/global-administrators`). Do not route tenant reads through projection actors or the generic EventStore query gateway.
- Query cursors are opaque and DataProtection-backed; cursor scopes include authenticated user and query context. Do not parse, expose, or log cursor contents.
- Read-model freshness uses EventStore `IReadModelFreshness` and `ReadModelFreshnessState`. Server read models persist `ProjectedAt`; `ToQueryResponseMetadata` emits `current/stale/unknown` via `X-Hexalith-Is-Stale`. `Aging` is dormant on the wire; `Refreshing` is UI-only.
- `ProjectedAt` measures last projection write, not global lag. Defaults are intentionally conservative; `ServedAt` must not be used as projection age.
- DAPR pub/sub is at-least-once and unordered. Consumers and projection handlers must be idempotent; use EventStore `MessageId` for duplicate detection and never treat `SequenceNumber` as global ordering.
- The Client package owns domain-specific event handler registration and an in-memory projection default only. Consuming services supply durable projection storage and shared dedup when scaling beyond one instance.
- Tenant configuration keys are consumer-owned namespaced strings, usually dot-prefixed (`billing.*`, `sample.*`). Consumers filter by their namespace and ignore others.
- MediatR pipeline order in Tenants is `ValidationBehavior` then `AuthorizationBehavior`; do not reorder or add generic behaviors casually.
- Domain exceptions map to RFC 7807 through registered exception handlers, specific handlers before generic.
- AppHost is the allowed repository-specific technical component. It wires EventStore, Tenants, Tenants UI, Sample, Memories, DAPR components, Keycloak, topic overrides, and Debug child build edges.
- AppHost changes require restarting `aspire run`; the Aspire app model is built at startup.
- DAPR access control is deny-by-default. Any sidecar/app-id/topic change must update DAPR access-control YAML and route tests.

### Identity Rules

- Identity = **`AggregateIdentity(TenantId, Domain, AggregateId)`** — build via the `TenantIdentity` factory: `ForTenant(id)`, `ForGlobalAdministrators()`. Constants: `DefaultTenantId = "system"`, `Domain = "tenants"`
- **Tenant ids and user ids are meaningful caller-supplied strings — NOT ULIDs.** (EventStore envelope ids like `MessageId` may be ULIDs; domain identifiers are not.) Do not `Guid.TryParse`/`Ulid.TryParse` a `TenantId`/`UserId`

### UI / UX Rules

- Tenants UI must use FrontComposer and Fluent UI Blazor V5 components. Do not introduce raw interactive HTML controls (`button`, `input`, `select`, `textarea`), raw forms, or raw table markup in `.razor` components.
- Route pages compose through FrontComposer page primitives (`FcPageHeader`, `FcPageLayout`, `FcAggregateListPage`, `FcAggregateDetailPage`). Do not add Tenants-owned page-root `<main>` wrappers, direct `PageTitle`, or raw route-level `<h1>`.
- Multi-region domain pages and panels group sibling titled content sections with `FluentAccordion`, `AccordionExpandMode.Multi`, and an initially expanded primary item. Do not hide the only primary content region behind an accordion.
- Use `FluentDataGrid` or FrontComposer grid primitives for data surfaces. Tenants-specific grids are allowed where FrontComposer does not provide cursor pagination, safety-column pinning, or required non-collapsing states.
- Express layout with Fluent/FrontComposer primitives such as `FluentStack`, `FluentGrid`, and FrontComposer layout modes. Inline layout styles are forbidden.
- Component CSS must not own theme primitives, semantic colors, broad layout/spacing/typography, or native control selectors. Any unavoidable layout/typography CSS requires an immediately preceding `/* fc-css-exception: ... */` marker with a real reason.
- Use Fluent V5 component parameters and Fluent 2 tokens only. Do not use legacy Fluent v4/FAST tokens such as `--type-ramp-*`, `--neutral-*`, `--accent-*`, or `--palette-*`.
- Keep raw semantic HTML only where Fluent has no equivalent (`section`, `header`, `nav`, `dl`, `ul`, `ol`, `li`, inline `a`). Structural raw `div`/`span` usage is budgeted by governance tests and should ratchet down.
- Every UI state must remain support-safe: never render bearer tokens, decoded JWT payloads, internal correlation IDs, stack traces, raw EventStore metadata, raw payloads, or cursor/ETag internals.
- Command UI must preserve the non-collapse truth-state model: accepted, projection-confirmed, and audit-available are distinct. SignalR is only a freshness nudge, never proof of success.
- Command flows must confirm from projection evidence before showing success. Terminal non-success states cannot become confirmed merely because unrelated projection data appears.
- `ReadModelFreshnessState.Unknown` and `Stale` generally fail closed for mutation actions; first-tenant create is a documented exception where unknown list freshness remains creatable.
- Memories search is an index match-set only. Search results must be hydrated through Tenants detail/list query paths before display; stale Memories data must not become row truth.
- UI localization uses `TenantsResources.resx` and `.fr.resx`; keep EN/FR key parity and use whole strings with placeholders, not runtime sentence fragments.
- Stable `data-testid` selectors and accessible names are part of the contract. Tests must not depend on row text, color alone, or incidental Fluent-generated markup.

### Testing Rules

- xUnit **v3** + **Shouldly** (`ShouldBe`, `ShouldThrow`) — **never raw `Assert.*`**. NSubstitute for mocks
- Test classes/files are **`{Class}Tests.cs` (plural)** — differs from Commons' singular `{Class}Test.cs`; methods use BDD names like `CreateTenant_with_no_prior_state_produces_TenantCreated`
- Use **`InMemoryTenantService`** (`Hexalith.Tenants.Testing`/Fakes) to test domain logic in isolation — it wraps `TenantAggregate.Handle` + `TenantState.Apply` with no infrastructure; `TenantTestHelpers` builds command envelopes; `InMemoryTenantProjection` is the read-model double. Reused across `Server.Tests`/`Testing.Tests`
- **Test tiers (from CI):** Tier 1 unit — `Contracts.Tests`, `Client.Tests`, `Testing.Tests`, `Sample.Tests` (no infra); **Tier 2 — `Server.Tests`** (needs `dapr init`, **blocking**); **Tier 3 — `IntegrationTests` (`Category!=Performance`)** (Aspire + Testcontainers, separate **non-blocking** `aspire-tests` job, `continue-on-error`); **Performance (`Category=Performance`)** — nightly schedule only
- **Run test projects individually** (CI invokes `dotnet test <project>` per project); use `.slnx` for restore/build only
- **Integration tests must assert state-store end-state** — e.g. `AssertPersistedOnceAsync<TenantCreated>(...)` checks the persisted `EventEnvelope` (`AggregateId`, sequence) after the DAPR round-trip. HTTP 201/202 alone is a smoke test, not an integration test
- **Coverage gates (`scripts/validate-coverage.py`):** line coverage **> 80%** (union across reports, scoped to the **5 package projects only** — host/AppHost/ServiceDefaults/samples excluded); **100% branch coverage** on isolation/auth files: `TenantAggregate.cs`, `GlobalAdministratorsAggregate.cs`, `ChangeUserRoleValidator.cs`. Add new isolation/auth logic to the gate
- All configured tests must pass before a story is complete

### Code Quality & Style Rules

- **`.slnx` only** (`Hexalith.Tenants.slnx`) — never create/use `.sln`; never run solution-level `dotnet test`
- `.editorconfig` sets `CA1062`/`CA1822`/`CA2007` to **warning** ("for scaffolding phase"); `TreatWarningsAsErrors` + CI `-warnaserror` promote them to build-breakers; `CA1014` disabled. **No SonarAnalyzer/StyleCop/Roslynator packages** (EventStore model, unlike Commons)
- `RestoreBuildInParallel=false` (`Directory.Solution.props`) — builds are intentionally serialized; don't "fix" it
- Containers via **.NET SDK container support — no Dockerfiles**. Only the host (`src/Hexalith.Tenants`) opts in (`EnableContainer=true`, `ContainerRepository=tenants` → `registry.hexalith.com/tenants`); defaults (alpine, non-root `app`, port 8080, OCI labels) in `Directory.Build.targets`. The 5 libraries ship as NuGet packages, not images
- The host references the EventStore **web host** project (known TODO) → `ErrorOnDuplicatePublishOutputFiles=false` is intentional; Tenants' own `appsettings` win
- **Never add package versions to `.csproj`** — all in `Directory.Packages.props`; `.csproj` uses `<PackageReference Include="…" />` without `Version`
- **Submodules: root-declared under `references/` only** (`references/Hexalith.EventStore`, `references/Hexalith.Commons`, `references/Hexalith.AI.Tools`, `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, `references/Hexalith.Memories`, `references/Hexalith.PolymorphicSerializations`). Never `--init --recursive` or initialize nested submodules; never modify submodule files without explicit approval (shared across Hexalith repos). **`Hexalith.Memories`** is consumed via source by `Hexalith.Tenants.UI` (`Hexalith.Memories.Contracts` + `Hexalith.Memories.Client.Rest`, for the Memories-backed tenant search) and by `samples/Hexalith.Tenants.Sample` + the AppHost; `$(HexalithMemoriesRoot)` is auto-detected in `Directory.Build.props`. `Hexalith.PolymorphicSerializations` may be pulled transitively. **Only ever call non-`[Experimental]` `MemoriesClient` APIs** (`SearchAsync` is safe; `IngestAsync`/`CreateTenantAsync` are `[Experimental("HXL001")]` and break the build)

### Development Workflow Rules

- **Conventional Commits required** (semantic-release): `feat`→minor, `fix`→patch, `feat!`/`BREAKING CHANGE:`→major; `docs`/`refactor`/`test`/`chore`/`perf`→no bump. Don't use `feat` for refactors (false minor bump + NuGet publish of 5 packages)
- **Branches:** `feat/…`, `fix/…`, `docs/…`. No direct commits to `main`
- **Release on merge to `main`** (`release.yml` → `npx semantic-release`): runs Tier 1+2 tests, then `scripts/pack-release-packages.py` packs the 5 packages, `validate-nuget-packages.py` + `validate-consumer-package-references.py` validate them → publish to NuGet, GitHub Release, update `CHANGELOG.md`
- **CI** (`ci.yml`) on push/PR to `main`: restore, build Release `-warnaserror`, package-consumer validation, Tier 1, `dapr init`, Tier 2, coverage gates; separate non-blocking Aspire (Tier 3) job; nightly performance job
- **Local run (slim/VM mode):** start DAPR `placement` + `scheduler` before `aspire run`, else actors fail ("did not find address for actor"); use `http://localhost:8080`; `EnableKeycloak=false` falls back to symmetric-key JWT

### Critical Don't-Miss Rules

- **Never** add `sealed`/XML docs to command/event/rejection records (plain records) — but **do** use `sealed record` + `<summary>` for query response DTOs
- **Never** throw for business failures in aggregates — return an `IRejectionEvent` (or `NoOp` for same-state)
- **Never** register `AddEventStoreServer`/`AggregateActor` or a tenant projection actor in the Tenants host — it is a domain service with in-process tenant query handlers and REST read endpoints
- **Never** reorder the MediatR pipeline (Validation → Authorization)
- **Never** treat `TenantId`/`UserId` as ULIDs; **never** treat `SequenceNumber` as global ordering — dedup on `MessageId`
- **Never** edit/delete events, projections, or the state store to fix data — submit compensating commands
- **Never** add copyright headers; **never** use `.sln`; **never** run solution-level `dotnet test`; **never** add versions to `.csproj`
- **Never** `--init --recursive` submodules or modify submodule files unsolicited; **never** hardcode the EventStore path (use `$(HexalithEventStoreRoot)`)
- **Always** `ConfigureAwait(false)` on awaits in host/Client code
- **Always** assert persisted state-store end-state in Tier 2/3 tests, not just status codes
- **Always** keep aggregates pure (events in, state rebuilt via `Apply`, no mutation in `Handle`)
- **Always** make event consumers idempotent (DAPR is at-least-once)
- **Don't** put secrets, bearer tokens, decoded JWT payloads, real tenant/user data, full payload dumps, or stack traces in docs, tickets, or logs (support-safety)

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code in `Hexalith.Tenants`
- Follow ALL rules exactly as documented
- When in doubt, prefer the more restrictive option
- This file complements (does not replace) `CLAUDE.md` at the repo root and the domain docs under `docs/` (see `event-contract-reference.md`, `idempotent-event-processing.md`, `compensating-commands.md`, `cross-aggregate-timing.md`, `production-auth-claim-contract.md`)

**For Humans:**

- Keep this file lean and focused on agent needs
- Update when the technology stack, analyzer policy, MediatR pipeline, test tiers, or coverage gates change
- Remove rules that become obvious over time

Last Updated: 2026-06-02
