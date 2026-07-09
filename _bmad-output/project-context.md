---
project_name: 'Hexalith.Tenants'
user_name: 'Administrator'
date: '2026-06-29'
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
rule_count: 118
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **.NET 10 / C#** — SDK pinned to `10.0.301` with `rollForward: latestPatch`; all owned projects target `net10.0`; `Nullable`, `ImplicitUsings`, `LangVersion=latest`, and `TreatWarningsAsErrors=true` are root defaults.
- **Solution/build** — `Hexalith.Tenants.slnx` only; `MSBuild.rsp` and `Directory.Solution.*` force single-node serialized builds (`-m:1`, `BuildInParallel=false`, `RestoreBuildInParallel=false`).
- **Hexalith platform dependencies** — `Hexalith.EventStore` packages pinned to `3.19.0`; `Hexalith.Memories` packages pinned to `1.31.1`. Debug uses source `ProjectReference` when available; Release uses NuGet packages for package-capable libraries.
- **DAPR** — DAPR SDK packages `1.18.4`; CI installs DAPR CLI/runtime `1.18.0` (the shared `domain-ci` default).
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

- Use xUnit v3 and Shouldly (`ShouldBe`, `ShouldThrow`, `Should.ThrowAsync`); do not use raw `Assert.*` in new tests. A few old scaffolding smoke tests still exist and should not be copied.
- Use NSubstitute for mocks and bUnit for Blazor/Fluent components. Fluent component tests should derive from the local Fluent bUnit setup and use loose JS interop when rendering Fluent UI.
- Test classes/files use plural `{Class}Tests.cs`; behavior names should stay descriptive and scenario-focused.
- Run tests per project, matching CI. Use `.slnx` for restore/build only; do not make solution-level `dotnet test` the default.
- CI Tier 1 blocking tests: `Contracts.Tests`, `Client.Tests`, `Testing.Tests`, `UI.Tests`, and `Sample.Tests`.
- CI Tier 2 blocking tests: `Server.Tests` after `dapr init`.
- CI Tier 3 Aspire tests: `IntegrationTests` with `Category!=Performance`, non-blocking `continue-on-error`; `Category=Performance` runs only on nightly schedule.
- Integration tests must assert persisted state-store/read-model end state, headers, projection metadata, or topology behavior. HTTP status alone is only a smoke signal.
- Coverage gate uses unioned Cobertura reports. Overall line coverage is `>80%` scoped to four package projects: `Contracts`, `Client`, `Server`, `Testing`. The published `.Aspire` helper is not in the current line-coverage scope.
- Branch coverage gate is `100%` for isolation/auth files: `TenantAggregate.cs`, `GlobalAdministratorsAggregate.cs`, and `ChangeUserRoleValidator.cs`. Add new isolation/auth logic to the gate.
- Domain logic tests should prefer `Hexalith.Tenants.Testing`: `InMemoryTenantService`, `TenantTestHelpers`, `TenantIsolationTestHelpers`, and `InMemoryTenantProjection`.
- UI command tests must cover validation before submit, fail-closed availability, projection-confirmed success, non-collapse lifecycle states, SignalR nudge-only behavior, support-safe copy, EN/FR resource parity, and focus/live-region behavior where relevant.
- Query/freshness tests must cover `X-Hexalith-Is-Stale`, `X-Hexalith-Served-At`, `X-Hexalith-Projection-Version`, `304` with freshness headers, unknown freshness, stale freshness, and conservative threshold behavior.
- If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility locally, use the built xUnit v3 executable fallback for that test assembly and record the fallback.
- All configured tests relevant to a story must pass before completion; document any blocked validation with the exact blocker.

### Code Quality & Style Rules

- Use `Hexalith.Tenants.slnx` only. Do not create `.sln` files, and do not run solution-level `dotnet test` as the default validation path.
- Keep package versions centralized in `Directory.Packages.props`; `.csproj` files should use `<PackageReference Include="..." />` without `Version`.
- Respect intentional serialized builds from `MSBuild.rsp` and `Directory.Solution.*` (`-m:1`, `BuildInParallel=false`, `RestoreBuildInParallel=false`). Do not "fix" this into parallel restore/build.
- Root defaults make warnings build-breaking: `TreatWarningsAsErrors=true`, CI `-warnaserror`, nullable enabled, implicit usings enabled, latest language version. Fix analyzer findings instead of suppressing them casually.
- Analyzer policy is intentionally lightweight. Do not add SonarAnalyzer, StyleCop, Roslynator, or formatting packages unless explicitly requested.
- Containers use .NET SDK container support, not Dockerfiles. Only `src/Hexalith.Tenants` is the container app (`EnableContainer=true`, `ContainerRepository=tenants`); libraries ship as NuGet packages.
- Published package surface is exactly five packages: `Contracts`, `Client`, `Server`, `Testing`, and `Aspire`. `Hexalith.Tenants` and `Hexalith.Tenants.UI` are container/application projects, not NuGet packages.
- Debug builds may use source `ProjectReference`s for available Hexalith libraries; Release builds should consume package-capable shared libraries through NuGet package references.
- Source-only shared references are intentional where packages are not yet available, such as EventStore web host and FrontComposer Contracts/Shell. Do not convert them blindly.
- The host's `ErrorOnDuplicatePublishOutputFiles=false` is intentional while it references the EventStore web host; Tenants appsettings should win.
- Keep Tenants domain-focused. Do not add reusable hosting, serialization, persistence, UI scaffolding, test harness, or cross-domain boilerplate here when it belongs in `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Commons`, `Hexalith.Builds`, or another shared module.
- Submodules are root-declared under `references/` only. Never use recursive submodule init, and do not modify submodule files without explicit approval.
- Use MSBuild root properties such as `$(HexalithEventStoreRoot)`, `$(HexalithFrontComposerRoot)`, and `$(HexalithMemoriesRoot)`; do not hardcode local absolute paths.
- Only call non-experimental Memories APIs from Tenants. `SearchAsync` is safe; experimental ingestion/tenant creation APIs should not be used from this repo.
- Do not add copyright/license headers to new files.

### Development Workflow Rules

- Use Conventional Commits. `feat` triggers a minor release, `fix` triggers a patch release, and `feat!` or `BREAKING CHANGE:` triggers a major release. Do not use `feat` for refactors or test-only work.
- Use branch names like `feat/...`, `fix/...`, or `docs/...`; do not commit directly to `main`.
- CI runs on push/PR to `main`: restore, Release build with warnings as errors, package metadata/consumer validation, Tier 1 tests, DAPR init, Tier 2 tests, and coverage gates.
- Release is gated on CI: the Release workflow triggers via `workflow_run` only after a successful push-event CI run on `main` (it does not re-run the test tiers), then semantic-release derives the version from commit history, packs exactly five NuGet packages, validates packages and package-only consumers, publishes to NuGet, publishes the tenants container, creates the GitHub Release, and updates `CHANGELOG.md`.
- Run local tests by project in the same shape as CI. Use `.slnx` for restore/build, then targeted `dotnet test <test-project>`.
- Initialize only the required root-declared submodules under `references/`; never use recursive submodule initialization.
- For local distributed runs, use Aspire AppHost. Restart `aspire run` after AppHost, DAPR component, topic, or sidecar changes because the app model is built at startup.
- In slim/VM local mode, start DAPR `placement` and `scheduler` before `aspire run`; actor-backed dependencies can fail without them.
- The default local Tenants service URL is `http://localhost:8080`; when Keycloak is disabled, local auth falls back to symmetric-key JWT.
- AppHost topology changes must update DAPR access-control YAML, route/topic wiring, and tests together.
- Do not edit EventStore, projection, or state-store data during debugging. Reproduce with commands, inspect persisted envelopes/read models, and fix state through compensating commands.
- Before implementing UI work, read the Hexalith UX instructions and verify FrontComposer/Fluent conformance tests still represent the intended governance.
- Before changing persistence, projection freshness, or query behavior, read the relevant EventStore state/freshness conventions and preserve cursor opacity.
- Package or source-reference changes must preserve Release package-only consumer validation; Debug convenience references cannot leak into Release package consumers.
- Document any validation that cannot be run with the exact command and blocker, not a generic "not tested" note.

### Critical Don't-Miss Rules

- Never add `sealed` or XML parameter docs to command, success-event, or rejection-event records. Query response DTOs are the exception: use `sealed record` with XML summaries.
- Never throw exceptions for business failures inside aggregates. Return structured rejection events, or `DomainResult.NoOp()` only for explicitly modeled same-state requests.
- Never add I/O, async calls, service dependencies, or mutation to aggregate `Handle` methods. State changes come only from events applied through `Apply`.
- Never register `AddEventStoreServer`, `AggregateActor`, or a tenant projection actor in the Tenants host. Tenants is a domain service with in-process query handlers and REST read endpoints.
- Never reorder the MediatR pipeline. Validation runs before authorization.
- Never treat `TenantId` or `UserId` as GUIDs/ULIDs, and never treat `SequenceNumber` as global ordering. Domain identifiers are caller-supplied strings; deduplicate on EventStore `MessageId`.
- Never parse, expose, or log query cursors, ETags, JWT payloads, bearer tokens, raw EventStore metadata, raw event payload dumps, stack traces, or internal correlation data.
- Never edit/delete/rewrite events, projections, or state-store data to fix business state. Use compensating commands and verify projection evidence.
- Never use raw interactive HTML controls, raw forms, raw tables, route-level `PageTitle`, or page-root `<main>` wrappers in Tenants UI. Use FrontComposer and Fluent UI Blazor V5.
- Never treat SignalR notifications, HTTP 202/201, or command acceptance as proof of user-visible success. UI success requires projection-confirmed evidence.
- Never let stale Memories search data become tenant row truth. Search returns match candidates; hydrate rows through Tenants query endpoints.
- Never use `.sln`, solution-level `dotnet test`, package versions in `.csproj`, Dockerfiles for the Tenants container, or recursive submodule initialization.
- Never add cross-domain technical plumbing to Tenants when it belongs in shared Hexalith modules.
- Always keep DAPR consumers idempotent and topology/access-control YAML aligned with AppHost sidecars, app IDs, and topics.
- Always preserve Release package-only consumer validation when changing references, packaging, or shared dependency flow.
- Always run or document the relevant per-project tests before completing implementation work.

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

Last Updated: 2026-06-29
