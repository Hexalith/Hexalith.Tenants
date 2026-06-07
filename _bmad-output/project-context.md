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

- **.NET 10** — SDK pinned `10.0.300` (`rollForward: latestPatch`) in `global.json`; all projects target `net10.0`, `Nullable`+`ImplicitUsings` enabled, **`TreatWarningsAsErrors=true`**, `LangVersion=latest` (`Directory.Build.props`)
- **Domain plugin that runs ON `Hexalith.EventStore`** — references EventStore **source** via `ProjectReference`; `$(HexalithEventStoreRoot)` is auto-detected by 4-layout logic in `Directory.Build.props`. Never hardcode the EventStore path
- **DAPR SDK 1.17.9** — `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors(.AspNetCore)` (state store, pub/sub, actors); DAPR CLI/runtime `1.17.0` in CI
- **.NET Aspire 13.4.0** — Hosting, Redis, Docker, Azure AppContainers, Testing; **Keycloak + Kubernetes are preview**; DAPR orchestration via `CommunityToolkit.Aspire.Hosting.Dapr` (preview)
- **MediatR 14.1.0** (CQRS), **FluentValidation 12.1.1**, JWT Bearer 10.0.8, OpenAPI 10.0.8 / Swashbuckle.SwaggerUI 10.2.1
- **OpenTelemetry 1.15.x**, `Microsoft.Extensions.*` 10.0.8, Http.Resilience + ServiceDiscovery 10.6.0
- **Testing:** xUnit **v3** (`xunit.v3` 3.2.2), Shouldly 4.3.0, **NSubstitute 6.0.0-rc.1**, Testcontainers 4.12.0, Mvc.Testing 10.0.8, coverlet.collector 10.0.1, `Microsoft.NET.Test.Sdk` 18.6.0, YamlDotNet 18.0.0; `Aspire.Hosting.Testing` for E2E
- **All versions centralized** in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) — **self-contained, NOT inherited from `Hexalith.Builds`** (differs from `Hexalith.Commons`)
- **5 publishable NuGet packages:** `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, `.Aspire`

## Critical Implementation Rules

### C# Language & Contract Rules

- **Commands/events/rejections are plain `public record` with primary constructors — NOT `sealed`, NO XML docs** (e.g. `public record AddUserToTenant(string TenantId, string UserId, TenantRole Role);`). Match this exactly — do not add `sealed` or `<param>` docs (differs from EventStore's "sealed record + XML docs" rule)
- **Query *contracts* are `sealed class : IQueryContract`** with a `static string QueryType`; **query *response DTOs* are `sealed record` WITH `/// <summary>` docs** (e.g. `TenantDetail`, `TenantMember`). This split is intentional
- Events implement **`IEventPayload`**; rejection events implement **`IRejectionEvent`** (both from `Hexalith.EventStore.Contracts.Events`, supplied via a global `<Using>` in `Contracts.csproj`). Commands have **no marker interface**
- Enums carry a **`Unknown = 0` sentinel** + `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` (or a custom converter that defaults to `Unknown` on parse failure, e.g. `TenantStatusJsonConverter`) — serialize enums by name, fail safe
- **NO file copyright/license headers** — 2 of 225 files have one; do **not** add MIT/ITANEO headers (matches EventStore, opposite of Commons)
- **`ConfigureAwait(false)` on every awaited call** — used 100% consistently (55/55 await sites: 48 in the host, 7 in `Client`). `Server`/`Contracts`/`Testing`/`Aspire` are pure synchronous code (no awaits). CA2007 is `warning` in `.editorconfig` but `TreatWarningsAsErrors` + CI `-warnaserror` make a missing one break the build
- File-scoped namespaces, Allman braces, `using` outside namespace, System directives first; `_camelCase` private fields, `I`-prefixed interfaces, `Async` suffix (all `warning` in `.editorconfig`). Namespace = folder path
- Validate at boundaries with `ArgumentNullException.ThrowIfNull` (pervasive in handlers)

### Event-Sourcing & Domain Rules

- **Aggregates are pure functions:** static `Handle(TCommand, TenantState?, CommandEnvelope) → DomainResult` + instance `Apply(TEvent)` on `TenantState`. No I/O, no async — state is rebuilt by replaying events. **Never mutate state outside `Apply`**, never mutate inside `Handle`
- Two aggregates: **`TenantAggregate`** (domain `tenants`) and **`GlobalAdministratorsAggregate`** (domain `global-administrators`) in `src/Hexalith.Tenants.Server/Aggregates/`. Handle/Apply are discovered by **reflection convention** via `AddEventStore(typeof(TenantAggregate).Assembly)` — no manual registration
- **Business failures are rejection events, NOT exceptions** — return an `IRejectionEvent` (14 exist: `TenantAlreadyExists`, `TenantNotFound`, `UserAlreadyInTenant`, `UserNotInTenant`, `InsufficientPermissions`, `LastGlobalAdministrator`, `RoleEscalation` incl. `TenantRole.Unknown`, `TenantDisabled`, `TenantLifecycleStateAlreadySet`, `ConfigurationLimitExceeded`, `ConfigurationKeyNotFound`, …). Same-state requests return **`NoOp`** (no event) — e.g. `ChangeUserRole` to the current role, `SetTenantConfiguration` with an identical key+value
- **Never edit/delete/rewrite events, projections, or the state store to "fix" data.** Correct with **compensating commands** via `POST /api/v1/commands`, verified by `GET /api/v1/commands/status/{correlationId}`. Corrections append new events; history stays immutable. Corrective `AddUserToTenant`/`ChangeUserRole` must state the **explicit intended role** (removal events don't carry the old role)
- Authorization for sensitive ops requires **tenant owner OR trusted global administrator** (enforced in aggregates/validators + `AuthorizationBehavior`)

### DAPR, Eventing & Consumer Rules

- **DAPR pub/sub is at-least-once, not exactly-once** — consumers MUST be idempotent. `TenantEventProcessor` dedups by **`MessageId`** (EventStore-assigned at persistence) via a `ConcurrentDictionary`; on handler failure it releases the claim so a corrected redelivery re-runs
- The subscriber endpoint (`MapTenantEventSubscription`) returns **200** for processed/duplicate/unknown/unhandled events and a **server error only for invalid payloads** (so DAPR redelivers) — preserve this contract
- **`SequenceNumber` is aggregate-local ordering ONLY** — never treat it as global order across services, tenants, aggregates, topics, or redeliveries. Use `MessageId` for duplicate detection
- Make handlers idempotent as defense-in-depth: dictionary set/remove and property assignment are safe; counters, notifications, and list-append need external dedup or an outbox. `TenantDisabled`/`TenantEnabled` are **eventually-consistent availability signals**
- Consuming services subscribe to the shared **`tenants.events`** topic and filter by event type via typed `ITenantEventHandler<T>` — do **not** call back synchronously to the host per access/config decision. EventStore is the source of truth; the local projection (`ITenantProjectionStore` / `TenantLocalState`) is runtime state. The **Client package takes no Redis/SQL/broker dependency** — the consuming service supplies durable projection + shared dedup
- Tenant configuration is namespaced by a **consumer-owned dot-prefix** (e.g. `sample.`, `billing.`) — filter reads by your prefix; ignore other namespaces

### Host Composition & Framework Rules

- **The Tenants host is a domain service, NOT an EventStore server.** Do **not** call `AddEventStoreServer` or register server-side EventStore extensions in `Program.cs`. `AggregateActor`/`ETagActor` are hosted by EventStore only. **The `TenantsProjectionActor` is RETIRED** (corrected 2026-06-06): tenant reads are served **in-process** by the 5 `IDomainQueryHandler`s (`Program.cs:110-114`) dispatched via `TenantsQueryController` (`GET /api/tenants*`) and the SDK `/query` endpoint (`MapEventStoreDomainService()`). The UI BFF wraps those REST endpoints directly — never route tenant queries through a projection actor or the EventStore generic query gateway (see `architecture.md` Read-access transport note)
- **MediatR pipeline order is `Validation → Authorization`** — `AddOpenBehavior(typeof(ValidationBehavior<,>))` then `AddOpenBehavior(typeof(AuthorizationBehavior<,>))` (`Program.cs:78-82`). Two behaviors only (no logging behavior). Do not reorder (this differs from EventStore's Auth→Log→Validate)
- The command API (`POST /api/v1/commands`) is imported from EventStore via `AddApplicationPart(...CommandsController...)`; `TenantBootstrapHostedService` sends bootstrap commands to EventStore over DAPR HTTP
- Domain exceptions map to **RFC 7807** via registered `IExceptionHandler`s, **specific before generic** (`Program.cs:120-128`)
- Query cursors are **opaque, DataProtection-backed** (`TenantQueryCursorCodec`, `SetApplicationName("Hexalith.Tenants")`). Multi-replica/restart durability needs a shared persisted key ring (deferred — Epic 11); don't assume cursors survive across replicas yet
- FluentValidation `AbstractValidator<T>` validators reference **domain constants** (e.g. `TenantAggregate.MaxKeyLength`) and use `Cascade(CascadeMode.Stop)` on critical fields
- **AppHost changes require restarting `aspire run`** (app model built at startup); DAPR access control is deny-by-default

### Identity Rules

- Identity = **`AggregateIdentity(TenantId, Domain, AggregateId)`** — build via the `TenantIdentity` factory: `ForTenant(id)`, `ForGlobalAdministrators()`. Constants: `DefaultTenantId = "system"`, `Domain = "tenants"`
- **Tenant ids and user ids are meaningful caller-supplied strings — NOT ULIDs.** (EventStore envelope ids like `MessageId` may be ULIDs; domain identifiers are not.) Do not `Guid.TryParse`/`Ulid.TryParse` a `TenantId`/`UserId`

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
- **Submodules: root-level only** (`Hexalith.EventStore`, `Hexalith.Commons`, `Hexalith.AI.Tools`, `Hexalith.FrontComposer`, `Hexalith.Builds`). Never `--init --recursive` or initialize nested submodules; never modify submodule files without explicit approval (shared across Hexalith repos)

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
