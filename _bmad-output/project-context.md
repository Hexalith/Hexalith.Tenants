---
project_name: 'Hexalith.Tenants'
user_name: 'Jerome'
date: '2026-05-31'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'code_quality', 'workflow_rules', 'critical_rules']
existing_patterns_found: 47
rule_count: 322
status: 'complete'
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

### Core Runtime & Build
- **.NET 10.0** (SDK `10.0.300`, pinned in `global.json` with `rollForward: latestPatch`). Do not bump SDK in feature work. Coordinate any SDK bump with `Hexalith.EventStore/global.json` first.
- **C# `LangVersion=latest`**, `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`** (root `Directory.Build.props`) — every analyzer/CS warning is a build break.
- **Style/analysis** enforced by `.editorconfig` (at repo root) + built-in .NET 10 analyzers only. Unlike sibling `Hexalith.Commons`, Tenants does **not** pull in StyleCop/SonarAnalyzer/Roslynator packages and does **not** set `GenerateDocumentationFile=true` — do not add either as part of unrelated work.
- **Modern XML solution format**: use `Hexalith.Tenants.slnx` only — never create or use `.sln`.

### Package Management
- **Centralized package management** via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). Never add `Version=` inline on a `PackageReference`; version bumps go in `Directory.Packages.props` only.

### Hexalith.EventStore (Submodule, not NuGet)
- **EventStore is a git submodule** at `Hexalith.EventStore/`, referenced via the `HexalithEventStoreRoot` MSBuild property which auto-detects 4 layouts (Tenants-as-submodule-of-EventStore, EventStore-as-submodule-of-Tenants, sibling submodules in an app repo, fallback nested). This dual topology is intentional — do not switch to `PackageReference` or relocate the submodule.
- Initialize with `git submodule update --init` (no `--recursive`). Uninitialized submodules produce cryptic missing-target build errors.
- Tenants builds on EventStore types: `EventStoreAggregate<T>`, `CommandEnvelope`, `DomainResult`, `IEventPayload`, `IRejectionEvent`, `IQueryContract`, `CachingProjectionActor`.
- **Root-level submodules only**: `Hexalith.EventStore`, `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.FrontComposer`. Never run recursive submodule init — nested submodules break the build.

### DAPR
- **DAPR SDK `1.17.9`** — `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors`, `Dapr.Actors.AspNetCore` (keep on the same version family). Before bumping, verify Tier 2 (`Server.Tests`) and Tier 3 (`IntegrationTests`) pass against the new version.
- **`CommunityToolkit.Aspire.Hosting.Dapr 13.3.0-preview.1.260514-0647`** is the DAPR sidecar integration for Aspire — not the obsolete `Aspire.Hosting.Dapr` workload package. Treat the API as preview-sensitive.

### .NET Aspire
- **`Aspire.Hosting 13.3.5`** (stable) and **`Aspire.Hosting.Testing 13.3.5`**. AppHost still uses `Aspire.AppHost.Sdk/13.3.3`; verify SDK compatibility before changing Aspire package families.
- The following preview integrations are intentional because no stable equivalent currently exists: `Aspire.Hosting.Docker` + `Aspire.Hosting.Kubernetes` (`13.1.2-preview.1.26125.13`), `Aspire.Hosting.Keycloak` (`13.3.5-preview.1.26270.6`). Before promoting any to a stable version, verify `Aspire.AppHost.Sdk` compatibility.
- **Other Aspire packages**: `Aspire.Hosting.Azure.AppContainers 13.1.2` (Azure Container Apps deploy target), `Aspire.Hosting.Redis 13.1.1` (DAPR state store backing).
- **Aspire CLI is the orchestrator**. Do not install the obsolete Aspire workload (`dotnet workload install aspire`); use `Aspire.AppHost.Sdk` + Aspire CLI. AppHost lives in `src/Hexalith.Tenants.AppHost`; topology edits require an Aspire restart.

### Application Frameworks
- **MediatR `14.1.0`** — command/query pipeline (Tenants uses EventStore's `SubmitCommand`/`SubmitQuery` MediatR contracts).
- **FluentValidation `12.1.1`** (+ DI extensions) — used only for commands with complex structural constraints; most commands rely on Handle-method domain validation.
- **JWT Bearer `10.0.8`**, **Microsoft.AspNetCore.OpenApi 10.0.8`**, **Swashbuckle.AspNetCore.SwaggerUI 10.1.7**.
- **OpenTelemetry 1.15.x family** (OTLP 1.15.3, Hosting 1.15.3, AspNetCore 1.15.2, Http/Runtime 1.15.1). Stay aligned with the current pin; coordinated bumps only.
- **Microsoft.Extensions.* 10.x family** (Configuration.Binder 10.0.3, Hosting 10.0.0, Http.Resilience/ServiceDiscovery 10.6.0).

### Testing
- **`xunit.v3 3.2.2`** — this is xUnit **v3**, NOT v2. The API differs (lifecycle, assertions, runners). Never switch to `xunit` (v2 packages); they cannot coexist meaningfully.
- **Shouldly `4.3.0`** for assertions — never use `Assert.*` directly even though xUnit allows it.
- **NSubstitute `6.0.0-rc.1`** — RC pinned for .NET 10 compatibility. Allowed to bump only when 6.0.0 stable ships with verified .NET 10 support.
- **Testcontainers `4.10.0`**, **coverlet.collector `10.0.1`**, **Microsoft.NET.Test.Sdk `18.5.1`**, **Microsoft.AspNetCore.Mvc.Testing `10.0.8`**, **YamlDotNet `18.0.0`** (used for DAPR component YAML fixtures in tests).
- All test projects inherit `tests/Directory.Build.props` (`IsPackable=false`, `IsTestProject=true`, global `using Xunit`, suppresses `IDE1006;CA2007;xUnit1051`). Do not add per-file `using Xunit;`.

### Publishing
- **5 NuGet packages** published via semantic-release on merge to `main`: `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Aspire`, `.Testing`. Host projects (`AppHost`, `Hexalith.Tenants`, `ServiceDefaults`) are `IsPackable=false`.
- **Containers via .NET SDK** (no Dockerfiles). Opt in per project with `<EnableContainer>true</EnableContainer>` + `<ContainerRepository>image-name</ContainerRepository>`. Defaults from `Directory.Build.targets`: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`, registry `registry.hexalith.com`, user `app` (non-root), port 8080.

### Tooling Notes
- **Ripgrep is available in this workspace** — prefer `rg` / `rg --files` for code and file searches.

---

## Language-Specific Rules (C# / .NET 10)

### Brace Style
- **K&R brace style** — opening `{` on the same line as the declaration: `public class TenantAggregate : EventStoreAggregate<TenantState> {`. This is the codebase convention and diverges from `Hexalith.EventStore`'s Allman style — do not "fix" to Allman. New code must match the surrounding K&R style.

### Namespaces, Usings, Files
- **File-scoped namespaces only** (`namespace Hexalith.Tenants.Contracts.Commands;`) — enforced by `.editorconfig`.
- **`using` directives outside namespaces**; `System.*` directives first, then external, then internal — enforced by `dotnet_sort_system_directives_first = true`.
- **One type per file**, file name matches the type name exactly.
- **Folder structure mirrors namespace**: `Commands/`, `Events/`, `Events/Rejections/`, `Aggregates/`, `Projections/`, `Validators/`. Maximum 2 levels deep within a project.

### Naming
- **PascalCase** for types, methods, public/internal members, and constants (`MaxConfigurationKeys = 100`).
- **`I` prefix** for interfaces (`IRejectionEvent`, `IQueryContract`).
- **`_camelCase`** for private instance fields (warning-level rule in `.editorconfig`).
- **`Async` suffix** required on async methods (warning-level rule).
- **No `_` prefix on records** — records use PascalCase positional parameters.

### Records vs Classes
- **`record` for immutable contracts** — Commands (`public record CreateTenant(string TenantId, string Name, string? Description);`), Events (`public record TenantCreated(...) : IEventPayload;`), Rejections (implement `IRejectionEvent`), query contracts, and read DTOs.
- **`class` for stateful types** — Aggregates (`TenantAggregate`), State (`TenantState`), Projections, Validators.
- **No `class` keyword + `IEventPayload`** — events are always records.

### Handle Methods (Aggregates) — Hard Rules
- **`public static DomainResult Handle(TCommand, TState?, CommandEnvelope?)`** — signature with optional `CommandEnvelope` third parameter when the handler needs the actor's identity (`envelope.UserId`, `envelope.AggregateId`, `actor:globalAdmin` extension).
- **Pure function**: no I/O, no DAPR, no async, no captured state. Reflection-based discovery requires the `static` modifier.
- **`ArgumentNullException.ThrowIfNull(command)`** (and on `envelope` when present) at the top — guard clauses are not optional even though `.editorconfig` lowers CA1062 to warning.
- **Never throw for business rule violations** — return `DomainResult.Rejection([new XxxRejection(...)])` instead. Exceptions in Handle bypass EventStore's idempotency cache, causing duplicate-command replay.
- **`AggregateId` comes from `envelope.AggregateId`**, not from the command body. Prefer `TenantIdentity.DefaultTenantId`, `TenantIdentity.Domain`, `TenantIdentity.GlobalAdministratorsDomain`, and `TenantIdentity.GlobalAdministratorsAggregateId` instead of hard-coding identity literals.
- **`switch` expressions with `_ when` guards** are the idiomatic shape (see existing Handle methods).

### Apply Methods (State)
- **Instance methods on state class** that mutate fields directly — no validation, trust the event.
- **State class is mutable by design** — do not refactor `TenantState` to immutable records.

### Result Construction
- **Collection expressions** for event/rejection lists: `DomainResult.Success([new TenantCreated(...)])`, `DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)])`. Do not use `new[]` or `new List<>()`.

### Null Safety
- **Nullable reference types enabled globally**. New APIs must express nullability intentionally.
- **No `!` (null-forgiving operator)** except at proven framework/test boundaries where the invariant is obvious and local.
- **Use `is not null`** / `is null` pattern matching, not `!= null` / `== null`.

### Serialization
- **`System.Text.Json` only** — EventStore standard. Do not introduce Newtonsoft.Json or any other serializer.
- **`DateTimeOffset` for timestamps** (preserves timezone). Convention: `{Action}At` field name (e.g., `CreatedAt`, `DisabledAt`).
- **`DateTimeOffset.UtcNow`** as the source of "now" in Handle methods.

### XML Documentation
- **Not required** — `GenerateDocumentationFile` is NOT set in `Directory.Build.props`. Add XML docs only when they explain non-obvious invariants. Do not pad public types with `<summary>` boilerplate.

### Constants
- **`internal const`** for aggregate invariants (`MaxConfigurationKeys`, `MaxKeyLength`, `MaxValueLength`). Comments belong on the constant declaration if the value encodes a subtle interpretation (the existing `MaxValueLength` comment documents "1KB = 1024 characters, not bytes" — preserve this).

---

## Framework-Specific Rules (EventStore + DAPR + Aspire)

### Identity Scheme
- **Platform `TenantId` is `"system"`** — a static deployment prerequisite, not dynamic. Must be pre-registered in EventStore's `appsettings.json` domain service registry AND in the IdP's JWT claims (`eventstore:tenant = "system"`). The quickstart treats this as a hard prerequisite — agents must not assume it's auto-provisioned.
- **Tenant domain is `"tenants"`**. **AggregateId** is the managed tenant ID for `TenantAggregate`.
- **Global administrator domain is `"global-administrators"`** and the singleton aggregate ID is also `"global-administrators"`. Do not route global-administrator events as normal tenant-domain events.
- **Actor ID format**: `"{tenant}:{domain}:{aggregateId}"` → e.g., `"system:tenants:acme-corp"` or `"system:global-administrators:global-administrators"`.
- **Every event payload must include `TenantId` as a top-level field.** (Envelope `TenantId` is always `"system"`; consumers identify the managed tenant only from the payload field.)
- **IDs are ULIDs, not GUIDs.** `messageId`, `correlationId`, `aggregateId`, `causationId` must parse via `Ulid.TryParse`. Never `Guid.TryParse` — ULIDs and GUIDs share a 36-char shape coincidentally. This is inherited from EventStore (Epic 2 R2-A7).

### Convention-Driven Wiring (Reflection)
- **Aggregates and projections are auto-discovered** by EventStore from the **`Hexalith.Tenants.Server` assembly only**. Aggregates placed in `Hexalith.Tenants`, `.Client`, `.Contracts`, or `.Testing` are NOT registered — the failure mode is silent (no actor registered → command times out).
- **Handle/Apply methods are discovered by signature**, not attribute. Renaming `Handle` or changing the return type breaks dispatch.
- **Naming conventions verified by reflection tests** in `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`:
  - **Commands**: `{Verb}{Target}` (`CreateTenant`, `AddUserToTenant`).
  - **Events**: `{Target}{PastVerb}` (`TenantCreated`, `UserAddedToTenant`).
  - **Rejection events**: `{Target}{Reason}Rejection` + must implement `IRejectionEvent` (`TenantNotFoundRejection`, `RoleEscalationRejection`).
- Adding a command/event/rejection means the corresponding naming test validates it automatically — non-conforming names fail tests.

### Aggregates
- **Two aggregates**: `TenantAggregate` (per-tenant), `GlobalAdministratorsAggregate` (singleton, **plural "Administrators"** — easy to mistype).
- **Inherit `EventStoreAggregate<TState>`** from `Hexalith.EventStore.Client.Aggregates`. State classes live next to aggregates in `Server/Aggregates/`.
- **Handle method signatures: both arities supported.** `Handle(TCommand, TState?)` and `Handle(TCommand, TState?, CommandEnvelope)` both auto-dispatch. Use the 3-arg form only when you need `envelope.UserId`, `envelope.AggregateId`, or `actor:globalAdmin`. Don't grab envelope context from anywhere else when the signature is 2-arg.
- **Empty-tenant bootstrap exception**: `AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false` (first-user bootstrap path). Preserve this when modifying `TenantAggregate`.
- **`TenantAggregate` does NOT enforce a "must retain ≥1 owner" invariant.** Removing the last owner is allowed by design — UX surfaces `ownerCount==0` as a warning. Do not add the invariant; it blocks legitimate ownership-transfer flows.

### Projections
- **Four projections**: `TenantProjection`, `GlobalAdministratorsProjection`, `TenantIndexProjection` (cross-tenant fan-in), `TenantAuditProjection`.
- **Inherit `EventStoreProjection<TReadModel>`**. Apply methods discovered by reflection.
- **Use `CachingProjectionActor`** for projection state (built-in ETag via `ETagActor`).
- **Cross-tenant index `CachingProjectionActor` adoption is conditional** — Story 5.2 verifies the actor supports fan-in event processing (events from ALL tenant aggregates → one projection actor). Fallback is manual DAPR state store with ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`, max 3 retries on 409). Do not promote the conditional path to "decided" until verification ships.

### Domain Processor (CommandApi ↔ AggregateActor)
- **`Hexalith.Tenants` host must expose a domain processor route** (default `/process`). The EventStore `AggregateActor` invokes it via DAPR service-to-service call back to the CommandApi (architecture D9). Missing this route stalls the 5-step pipeline at Step 4 with no obvious error.
- **Domain service registration** in `appsettings.json` (`EventStore:DomainServices:tenants`) maps the DAPR AppId → CommandApi endpoint.

### Authorization (RBAC — Role-Based Access Control)
- **Layer 1 — API gate**: EventStore's `AuthorizationBehavior` in the MediatR pipeline + `ClaimsTenantValidator` + `ClaimsRbacValidator`. Validates JWT `eventstore:tenant` claim.
- **Layer 2 — Domain RBAC**: Inside aggregate `Handle` methods, check `state.Users[envelope.UserId]` against required `TenantRole` via `IsAuthorized(state, userId, requiredRole)`. Enforces Reader/Contributor/Owner hierarchy.
- **Layer 2b — Query-side row filtering**: `GetUserTenantsQuery` handler filters result rows based on requester scope (self / TenantOwner-of-target-tenant / GlobalAdmin).
- **GlobalAdmin override**: indicated by `actor:globalAdmin` extension metadata on `CommandEnvelope`; `IsGlobalAdmin(envelope)` returns true and Handle bypasses per-tenant RBAC. Never trust user-supplied claims — the extension is populated by EventStore's claims transformation.
- **Client-submitted reserved extensions are untrusted**. Never consume `actor:globalAdmin` or equivalent authority flags unless they came from trusted server-side claims transformation.
- **Use `envelope.UserId` (JWT `sub`) for user identity.** Never `name` or `email`.
- **Architectural boundary**: Tenants is the source of truth for membership — it cannot self-authorize. Layer 2 (domain RBAC in Handle) is canonical for Tenants' OWN authorization. EventStore's `IRbacValidator`/`ITenantValidator` interfaces exist for **consuming** services, not for Tenants. Refactoring Handle-method RBAC into a validator behavior introduces a circular dependency.

### Bootstrap
- **`TenantBootstrapHostedService`** reads `Tenants:BootstrapGlobalAdminUserId` from `appsettings.json` on startup and sends `BootstrapGlobalAdmin` through the full MediatR pipeline (validation + authorization).
- **Aggregate self-protects**: `GlobalAdministratorsAggregate.Handle(BootstrapGlobalAdmin, …)` returns `Rejection([new GlobalAdminAlreadyBootstrappedRejection(...)])` if any `GlobalAdministratorSet` event exists.
- **Not a public API endpoint** — startup config or CLI only. Do not expose as REST.
- **Multi-instance startup**: N-1 instances will receive bootstrap rejections — expected behavior. Log at `Information` level with `"Global administrator already bootstrapped, skipping"`. Do not log Warning/Error and do not retry.

### DAPR
- **Actor state via `IActorStateManager` only** — never bypass with `DaprClient` for aggregate actor state.
- **Domain services have no direct Redis/state-store/pub-sub references** — all infrastructure access flows through EventStore primitives.
- **Pub/sub uses CloudEvents 1.0.**
- **Resource naming** (convention-derived by EventStore's `NamingConventionEngine` — single source of truth):
  - AppId: `tenants`
  - State store: `tenants-eventstore`
  - Topic: `tenants.events` (single topic for all tenant events; consumers filter by event type)
  - Dead letter: `deadletter.tenants.events`
- **Access control is deny-by-default.** When adding service invocation paths, update the receiving service's DAPR config and verify caller app IDs.
- **DAPR slim/local mode** may require placement + scheduler processes before actor flows work. "Missing actor address" errors usually mean infrastructure isn't fully up — run `dapr init` first.

### Snapshot Configuration
- **`tenants` domain**: 50-event snapshot interval (NFR13 — 30s startup for 500K events). Configured in `appsettings.json`: `EventStore:Snapshots:DomainIntervals:tenants = 50`.
- **`global-administrators` domain**: default 100-event interval (low event volume, singleton aggregate). Do not override.
- **Snapshot interval has a floor of 10 events** enforced by EventStore's `SnapshotManager` — values below clamp silently.
- **Snapshot writes are advisory** — failures must not block the command pipeline.

### API Surface
- **Single command endpoint**: `POST /api/commands` (EventStore's `CommandsController`).
- **Query endpoints** on Tenants host: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`.
- **Internal query dispatch** uses EventStore's `SubmitQuery`/`QueryRouter` (MediatR). Query contracts implement `IQueryContract` with `QueryType`/`Domain`/`ProjectionType`. Controllers translate REST → `SubmitQuery`.
- **Query controllers are thin adapters**: validate route/query input, derive authenticated user from JWT `sub`, validate signed opaque cursors, then dispatch `SubmitQuery`. Query authorization and filtering belongs in projection/query handling, not controller branching.
- **Error responses follow RFC 7807 Problem Details.** Rejection events are mapped to HTTP status codes by a `RejectionToHttpStatusMapper` middleware (404 for not-found, 409 for conflict, 422 for other domain rejections). The `type` field carries the rejection event type name for programmatic consumer handling. New rejection events need a mapping registration — verify whether the mapper is reflection-driven or explicit before adding rejections that should map to a non-default (422) status.
- **JSON: camelCase** (`JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase`).
- **List responses**: `{ "items": [...], "cursor": "next-page-token", "hasMore": true }` — cursor-based pagination only, never offset/limit.
- **Cursors are signed, opaque, and scope-bound**. Never add offset/limit pagination or cursors that can be replayed across tenants/users/query shapes.
- **ETag pre-check** at controller level: `If-None-Match` → `304 Not Modified` (served by `CachingProjectionActor`).

### MediatR Pipeline (Command Path)
Order: `FluentValidation → AuthorizationBehavior → SubmitCommandHandler → CommandRouter → AggregateActor`. Do not reorder. See **Authorization** above for L1/L2/L2b layer responsibilities.

**AggregateActor 5-step checkpoint sequence** (each step has its own failure recovery — don't short-circuit):

1. **Idempotency check** — cached result by `CausationId`; resume in-flight pipelines from cached result.
2. **Tenant validation** — validates `TenantId` matches actor ID. Runs BEFORE state access (security-critical).
3. **State rehydration** — load snapshot + tail-only event replay → current state. Dead-letter on failure.
4. **Domain service invocation** — DAPR service-to-service call to `Hexalith.Tenants` `/process` endpoint. Dead-letter on failure.
5. **Event persistence + publication** — persist events atomically, snapshot if threshold met, publish via DAPR pub/sub. Drain reminder for failed publications.

### Configuration Limits (TenantAggregate)
- `MaxConfigurationKeys = 100` — entries per tenant
- `MaxKeyLength = 256` — characters
- `MaxValueLength = 1024` — characters (NOT bytes — `string.Length`, lenient for multi-byte)
- These are `internal const` on `TenantAggregate`. Validators **MUST reference the constants**, not duplicate the literals. `SetTenantConfigurationValidator` is the existing pattern.

---

## Testing Rules

### Three-Tier Test Model
- **Tier 1 — Pure unit** (no infrastructure): `Contracts.Tests`, `Client.Tests`, `Testing.Tests`. Aggregate Handle/Apply tested as pure functions via `aggregate.ProcessAsync(envelope, currentState)` — no DAPR, no actors, no mocks of EventStore internals. Target: ≤10ms per test (NFR2).
- **Tier 2 — DAPR integration**: `Server.Tests`. Requires `dapr init` + Docker. Verifies pipeline behavior, projection writes, `AuthorizationBehavior` rejection paths. **Must inspect state-store end-state** (Redis key contents, persisted CloudEvent bodies) — NOT only API return codes or mock call counts. Mocking `DaprClient` for Tier 2 defeats the purpose.
- **Tier 3 — Aspire E2E**: `IntegrationTests`. Full topology via Aspire AppHost.
- **NFR13 perf**: separate scheduled category (nightly). 500K events seeded → cold-start actor → assert rehydration ≤30s. Never per-PR.
- **CI commands**:
  - Tier 1: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` (and `Client.Tests`, `Testing.Tests`)
  - Tier 2: `dapr init` first, then `dotnet test tests/Hexalith.Tenants.Server.Tests/`
  - Tier 3: `dotnet test tests/Hexalith.Tenants.IntegrationTests/` after DAPR + Aspire ready

### Frameworks & Assertions
- **xUnit v3** — `[Fact]`, `[Theory]` + `[InlineData(...)]`. Global `using Xunit;` from `tests/Directory.Build.props`; never per-file.
- **Shouldly only**: `ShouldBe`, `ShouldNotBeNull`, `ShouldBeOfType<T>()`, `ShouldBeInRange`, `Should.Throw<TException>(() => …)` for expected exceptions. Never `Assert.*`.
- **Typed event assertion pattern**: `IEventPayload evt = result.Events[0].ShouldBeOfType<TenantCreated>(); ((TenantCreated)evt).TenantId.ShouldBe("acme");` — `ShouldBeOfType` + cast + property assertions.
- **Every test contains at least one Shouldly assertion.** No `Assert.True(true)` placeholders or assertion-free tests.
- **NSubstitute** for mocks; prefer hand-rolled fakes from `Hexalith.Tenants.Testing` / `Hexalith.EventStore.Testing` first.
- **Tier 2/3 helpers** (use before introducing new mocks): `InMemoryTenantService`, `InMemoryTenantProjection` (Tenants.Testing); `TestServiceOverrides`, `FakeProjectionActor`, `FakeETagActor` (EventStore.Testing).
- **Suppressed in `tests/Directory.Build.props`** (`NoWarn`): `IDE1006`, `CA2007`, `xUnit1051`. Don't re-enable per project.

### Test Naming & Layout
- **Test class**: `{TypeUnderTest}Tests.cs` (plural — `TenantAggregateTests.cs`). Diverges from sibling `Hexalith.Commons` (singular `Test`).
- **Test method**: `snake_case_with_PascalCase_for_type_names` — e.g., `CreateTenant_with_no_prior_state_produces_TenantCreated`. Codebase convention; `IDE1006` is suppressed in test projects to allow it.
- **Test layout mirrors source**: `tests/Hexalith.Tenants.{Project}.Tests/{Feature}/{Type}Tests.cs` mirrors `src/Hexalith.Tenants.{Project}/{Feature}/{Type}.cs`.
- **Test data / dummy classes co-located** with the tests that use them (in the same feature folder).
- **Comment style**: Given/When/Then prose comments where the boundary isn't obvious; existing tests use this — keep it.

### CommandEnvelope Test Helper
- **Use the `CreateCommand<T>(command, actorUserId, isGlobalAdmin)` helper** in `TenantAggregateTests.cs` (and equivalents). Centralizes platform tenant/domain literals and `actor:globalAdmin` extension setup. Do not construct `CommandEnvelope` inline ad-hoc.

### Mandatory Test Categories

**Conformance — `tests/Hexalith.Tenants.Testing.Tests/ConformanceTests.cs`** (Tier 1, reflection-driven):
- For each command in `Contracts.Commands`: execute against real `TenantAggregate`, capture events; execute the same command against `InMemoryTenantService`, capture events; assert **identical event sequences AND identical order** (not just set equality). Rejection events are also covered.
- Adding a new command requires no manual test change.
- **Never `[Skip]` or disable** — even for in-progress commands. Release blocker.

**Event serialization round-trip — `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`** (Tier 1):
- For each event type in `Contracts.Events/`: populate **all fields with non-default values** → `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes` (use the shared `JsonSerializerOptions` factory; never inline `new JsonSerializerOptions()`) → deserialize → deep-equality assertion.
- **Post-v1.0**: ALSO deserialize from golden fixtures at `tests/Hexalith.Tenants.Contracts.Tests/Fixtures/{EventType}.json`. New events require a fixture commit. Consider a JSON-schema fingerprint snapshot to catch property-name drift.

**Cross-tenant isolation** (NFR5 — zero leaks, three-tier defense):
- **Tier 1** (`Server.Tests/Aggregates/`): Handle method rejects when `envelope.UserId` is not in `state.Users`.
- **Tier 2** (`Server.Tests/Authorization/`): `AuthorizationBehavior` rejects requests with mismatched tenant scope in JWT claims.
- **Tier 2/3** (`IntegrationTests/CrossTenantIsolationTests.cs`): API-level — create TenantA/UserX-Owner, TenantB/UserY-Owner, attempt cross-tenant ops with the wrong scope, assert `403 Forbidden` on command AND query paths.
- **Additionally assert**: signed opaque cursors reject tampering and scope mismatch; pagination cursors don't encode or replay other tenants' IDs; error response bodies (RFC 7807 Problem Details) don't leak cross-tenant data.

**Query pagination/cursor behavior**:
- Cover cursor tampering, scope mismatch, tenant/user mismatch, page-size clamping, disabled tenant behavior, and orphaned membership behavior when touching query code.
- Cursor tests should verify both rejection shape and absence of cross-tenant identifiers in the response body.

**Projection write safety**:
- Projection write tests must verify ETag/concurrency behavior and final stored state, not only returned HTTP status or handler result.
- Retry tests must prove optimistic-concurrency conflicts merge or preserve existing projection state according to the projection-specific policy; never accept last-write-wins as an incidental outcome.

**Auth/RBAC changes**:
- Authorization changes require coverage at the API gate, aggregate/domain RBAC, global-admin override, and query-side row filtering layers.
- Tests must prove client-supplied `actor:globalAdmin` or reserved extensions cannot grant authority.

**Naming convention — `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`** (Tier 1):
- Reflection over `Contracts.Commands`/`.Events`/`.Events.Rejections` verifies verb/past-verb/`Rejection` suffix conventions.
- **Also verify**: every type in `Events.Rejections` implements `IRejectionEvent` (interface check, not just name pattern).

**Bootstrap contract** (Tier 2/3):
- Assert **exactly-one-success across N parallel instances** — N-1 `GlobalAdminAlreadyBootstrappedRejection` outcomes are the contract; do not assert "bootstrap never rejects."
- **Config validation at bind**: `Tenants:BootstrapGlobalAdminUserId` must be a non-empty string; tests verify malformed config is rejected before host startup, not at first command.

### Per-Change Checklists

**Adding a new command** (`{Verb}{Target}` in `Contracts.Commands`):
1. Handle method in the appropriate aggregate (`TenantAggregate` or `GlobalAdministratorsAggregate`).
2. Handle tests in `Server.Tests/Aggregates/{Aggregate}Tests.cs` — cover Success, Rejection, NoOp paths.
3. Conformance test auto-covers — extend `InMemoryTenantService` so the conformance assertion stays green.
4. Naming convention test auto-covers (verb-first PascalCase).
5. If new rejection types: register HTTP mapping in `RejectionToHttpStatusMapper` and add Tier 2/3 integration test asserting the status code.

**Adding a new event field**:
1. Serialization round-trip auto-covers IF the test populates all fields with non-default values (verify the fixture builder is updated).
2. Post-v1.0: regenerate the golden fixture.
3. If the field is materialized into a read model: update `TenantProjection.Apply({Event})` tests and the corresponding `TenantReadModel` deep-equality assertions.

### Async / Eventual-Consistency Tests
- **Wait for observable state, not time.** Poll the projection or state store with a bounded timeout; never `Thread.Sleep(N)` or `await Task.Delay(N)` as a sync mechanism.
- For SignalR / projection nudge tests: re-query REST until expected state appears (or timeout), don't trust the nudge payload as data.

### Quarantine Policy
- **No `[Fact(Skip = "...")]` without a tracking issue.** Skip comment must reference an open issue ID.
- Quarantined tests use `[Trait("Category", "Quarantined")]` and are filtered out of the main blocking CI lane (matches sibling `Hexalith.FrontComposer` convention). Quarantine is a temporary triage state, not a release path.

### What NOT to Do in Tests
- Never use `Assert.*` — Shouldly only.
- Never write Tier 2/3 tests that assert only HTTP status codes or mock-call counts.
- Never `Guid.TryParse` on `messageId`/`correlationId`/`aggregateId`/`causationId` — they're ULIDs.
- Never disable conformance / naming-convention / serialization round-trip tests to ship a change.
- Never seed 500K events in a per-PR test — that's the scheduled perf category.
- Never assume bootstrap succeeds exactly once across parallel instances — assert exactly-one-success.
- Never construct `CommandEnvelope` inline — use the `CreateCommand<T>` helper.
- Never assert on raw log message text — use semantic logging properties or test-only event hooks.
- Never inline `new JsonSerializerOptions()` — use the shared factory.
- Never hard-code local paths in fixtures — embedded resources or relative paths only.
- Never `Thread.Sleep` / `Task.Delay` to wait for async state — poll observable state.

---

## Code Quality & Style Rules

### Formatting & Analyzer Governance
- Keep `.editorconfig` formatting: CRLF, UTF-8, 4-space indentation, trimmed trailing whitespace, and final newline.
- Keep warnings clean because `TreatWarningsAsErrors=true`; fix compiler/analyzer warnings instead of suppressing locally.
- Do not add StyleCop, SonarAnalyzer, Roslynator, XML-documentation enforcement, or analyzer packages from sibling repos unless the task explicitly changes Tenants governance.

### Project Dependency Direction (Hard Architectural Constraint)
**Rule: never add a reference that flows away from `Contracts`.** The allowed edges below; any reverse edge fails the build and breaks semver.

- `Hexalith.Tenants.Contracts` → references only `Hexalith.EventStore.Contracts`. No `Hexalith.Tenants.*` deps.
- `Hexalith.Tenants.Client` → `Contracts` + `Hexalith.EventStore.Client`.
- `Hexalith.Tenants.Server` → `Contracts` + `Hexalith.EventStore.Server`.
- `Hexalith.Tenants.Testing` → `Server` + `Contracts` + `Hexalith.EventStore.Testing`.
- `Hexalith.Tenants` (host) → `Server` + `Contracts` + `ServiceDefaults`.
- `Hexalith.Tenants.Aspire` → `Contracts` + `Client`.
- `Hexalith.Tenants.AppHost` → orchestrates the host (not a NuGet).
- `Hexalith.Tenants.ServiceDefaults` → shared host config (OpenTelemetry, DI extensions).

**Cross-module pairing**: Tenants.{Tier} references EventStore.{Tier} where `{Tier} ∈ {Contracts, Client, Server, Testing}`. Never cross tiers (e.g., Tenants.Contracts → EventStore.Server is forbidden).

### Type Location Rules (Most Common Agent Conflict — Resolve via This Table)
| Type Category | Project | Why |
|---|---|---|
| Commands (`CreateTenant`, …) | `Contracts/Commands/` | Consumed by callers |
| Events (`TenantCreated`, …) | `Contracts/Events/` | Consumed by subscribers |
| Rejection events (`*Rejection : IRejectionEvent`) | `Contracts/Events/Rejections/` | Part of the event contract |
| Identity helpers (`TenantIdentity`) | `Contracts/Identity/` | Public contract surface |
| Enums (`TenantRole`, `TenantStatus`, `AuditEventCategory`) | `Contracts/Enums/` | Consumed by callers |
| Query contracts (`GetTenantQuery : IQueryContract`) | `Contracts/Queries/` | Consumed by callers |
| Aggregates (`TenantAggregate`) | `Server/Aggregates/` | Auto-discovered domain logic |
| State classes (`TenantState`) | `Server/Aggregates/` | Aggregate-private |
| Projections (`TenantProjection`) | `Server/Projections/` | Auto-discovered |
| Read models (`TenantReadModel`, `TenantAuditReadModel`) | `Server/Projections/` | Projection output |
| FluentValidation validators | `Server/Validators/` | Pipeline-discovered |
| Controllers / Bootstrap services | `Hexalith.Tenants/` (host) | Endpoint surface |
| Client DI extensions | `Client/Registration/` | `Add*` extensions |
| In-memory fakes (`InMemoryTenantService`, `InMemoryTenantProjection`) | `Testing/Fakes/` or `/Projections/` | Test-only |
| Test helpers (`TenantTestHelpers`) | `Testing/Helpers/` | Test-only |

Placing a type in the wrong project breaks dependency direction or auto-discovery. Default rule: if any external project would consume it → `Contracts`; otherwise → `Server`.

### Public API Surface (NuGet Packages)
- **Both `Contracts` AND `Server` are published NuGets** — `Server`'s public types (aggregates, state classes, projections, read models) are part of the consumer-facing surface. Refactors affecting public members trigger semver. Do not treat Server as "internal."
- **Phase 1 (pre-v1.0)**: event/command schemas may change; document instability in CHANGELOG.
- **Phase 2 (post-v1.0)**: additive only. Breaking changes require a new event type + backward-compatible deserialization gated by `eventTypeName` + `domainServiceVersion`. Use `feat!:` Conventional Commit only when truly removing/renaming public API.
- **Contracts is the highest blast-radius package** — every change should be PR-reviewed against the event-contract-reference doc.

### `sealed` Defaults
- **Default to `sealed` for new types.** Exceptions (intentionally open because EventStore reflection/inheritance requires it):
  - **Aggregates** — extend `EventStoreAggregate<T>` (open).
  - **State classes** — mutated by `Apply` methods (open).
  - **Projections** — extend `EventStoreProjection<T>` (open).
  - **Read models** — projection outputs (open by convention).
- All other types (helpers, validators, DTOs, options classes, internal records) — `sealed`.

### Rejection Event Payloads
- **Carry structured data only** — IDs, enums, counts. Never English strings, never localized text, never user-facing prose.
- User-facing error text is composed at the HTTP boundary by `RejectionToHttpStatusMapper` from the structured payload + a template (RFC 7807 `title`/`detail`).
- **Rationale**: rejection events are persisted in the event store and live forever — putting English in payloads commits today's wording to permanent history.
- The `type` field in the HTTP error body uses the rejection event class name, so class names must be programmable identifiers callers can switch on.

### Logging & Telemetry
- **Structured logging only** — semantic parameters, never `string.Format` or interpolation in log templates.

```csharp
// ✓ DO — semantic parameters resolved by the logger
logger.LogInformation("Tenant created: TenantId={TenantId}, Name={Name}", tenantId, name);

// ✗ DON'T — string interpolation defeats structured logging
logger.LogInformation($"Tenant {tenantId} created with name {name}");
```

- **Never log**: event payload content, command payload content, secrets, JWT tokens, user-controllable display names as "trusted identity," PII.
- **Always include** `CorrelationId` (provided by EventStore's middleware) and use envelope metadata: `tenant`, `domain`, `aggregate`, `causationId`, `commandType`/`eventType`, `stage`.
- **Log levels**:
  - `Information` — lifecycle (host start, bootstrap result, command accepted, projection applied).
  - `Warning` — advisory failures (snapshot write failed, command status update failed).
  - `Error` — pipeline/infrastructure failures only.
- **Domain rejections are NOT errors.** EventStore's pipeline already emits Information/Debug logs for rejections — do not add additional logging in Handle methods. Adding `LogError` for a domain rejection creates false alarms.
- **Use `EventStoreActivitySource` and source-generated logger patterns** when surrounding code does. Don't create ad-hoc `ActivitySource` instances. Every command/event activity carries `correlationId`.

### Security & Sanitization
- **Sanitize `CommandEnvelope.Extensions` at the API boundary** before they enter the processing pipeline (architecture SEC-4). Don't trust extension content from external callers.
- **Use `sub` as the authenticated user identifier** — never `name`, `email`, or other user-controllable claims.
- **Tenant validation happens BEFORE state rehydration** (see Framework → MediatR Pipeline Step 2). Don't reorder; this is a security-critical checkpoint.
- **DAPR access control is deny-by-default**; new service-to-service paths require explicit caller AppId in the receiver's config.
- **Do not introduce direct Redis, database, broker, or storage coupling** into domain/server packages when EventStore/DAPR abstractions already own the boundary.

### Extension Methods (DI Registration)
- **Pattern**: `AddHexalithTenants(this IServiceCollection services)` + overload `(this IServiceCollection services, Action<HexalithTenantsOptions> configureOptions)` — both return `IServiceCollection` for chaining. Match the existing pattern in `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`.
- Public DI extensions DO carry XML doc comments even though `GenerateDocumentationFile` isn't set (`<summary>`, `<param>`, `<returns>`) — match the existing style for new public extension methods.
- **`ArgumentNullException.ThrowIfNull(services)`** at the top of every extension — see existing pattern.
- **Aspire extensions** in `Hexalith.Tenants.Aspire/` follow `AddHexalithTenants(this IDistributedApplicationBuilder builder, …)`.
- **Never scatter inline DI setup** across host `Program.cs`. Wrap in a project-specific `Add*` extension and call once.

### Comments
- Explain non-obvious invariants, story/ADR cross-references, or values that encode subtle interpretation. The gold standard is the `MaxValueLength = 1024` comment in `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` lines 14-16 documenting "chars not bytes."
- **Do NOT add comments that restate the code.** XML docs are not enforced — only add `<summary>` when it carries non-trivial context OR you're matching the existing pattern on public DI extensions.
- Cite story IDs / FR numbers / ADR references (`FR23`, `Epic 2 R2-A7`, `D11`) when a rule's rationale lives in the planning artifacts.

### Per-Change Checklist (Adding a New Public Type)
1. Decide Contracts vs Server using the Type Location table (default: Contracts if external consumers will reference it).
2. Verify no reverse dependency edge is introduced.
3. Add `sealed` unless the type is on the open-by-design list.
4. If the type is a rejection event: structured payload only; add `RejectionToHttpStatusMapper` registration if non-default (422) status is needed.
5. If the type is a NuGet-published public surface: confirm semver impact (additive = patch/minor, breaking = `feat!:`).
6. CHANGELOG entry is generated by semantic-release from the Conventional Commit — write the commit message accordingly.

### File / Project Hygiene
- **`.editorconfig` governs whitespace/encoding/indent** — enforced by the toolchain; not a manual rule.
- **`.csproj.lscache` files (MSBuild local caches) should be in `.gitignore`.** Current tree has several tracked (`Hexalith.Tenants.Server.csproj.lscache` etc.) — do not add new ones; cleanup is a separate PR.
- **Do not commit** `bin/`, `obj/`, `TestResults-Coverage/`, `nupkgs/`, `eventstore-*.log`. They're untracked artifacts and should stay that way.

---

## Development Workflow Rules

### Repository Root
- Work from the actual repo root `Hexalith.Tenants/`, not the parent BMad workspace. Solution, package, source, tests, and repo-local `_bmad-output/` files live under that directory.
- Use `Hexalith.Tenants.slnx` for solution-level commands.

### Conventional Commits (Required — semantic-release Depends On It)
Format: `<type>(<optional scope>): <description>`

| Type | Version bump | When to use |
|---|---|---|
| `feat` | **minor** | New public API only — not refactors, not internal changes |
| `fix` | **patch** | Bug fix (behavior change for existing API) |
| `refactor` | none | Internal restructure, no API change |
| `test` | none | Adding/updating tests |
| `docs` | none | Docs/comments only |
| `chore` | none | Tooling, CI, build scripts |
| `perf` | none (use carefully) | Performance improvement |
| `style` | none | Formatting only |
| `build`/`ci` | none | Build infra, GitHub Actions |

**Breaking changes**: append `!` after the type (`feat!:`) OR add a `BREAKING CHANGE:` footer in the body. Triggers a **major** version bump.

**Description rules**:
- Imperative mood, lowercase, no trailing period.
- Total length under 50 chars (including `type(scope): ` prefix when possible).
- Body wrapped at 72 chars.

**Examples** (matching project CLAUDE.md):
- `feat(contracts): add TenantConfigurationSet command`
- `fix(server): prevent duplicate user addition to tenant`
- `docs: update quickstart with DAPR init prerequisites`
- `chore(ci): replace MinVer with semantic-release`
- `feat!: rename TenantAggregate state shape`

**Anti-patterns**:
- Never use `feat:` for a refactor — it ships a minor version + a NuGet publish for no API change.
- Never use `fix:` for new behavior — use `feat:`.
- Never combine multiple types in one commit — split.

### Branch Naming
- `feat/<short-description>` — features and enhancements
- `fix/<short-description>` — bug fixes
- `docs/<short-description>` — documentation changes
- Story branches may include the story ID: `feat/2-3-tenant-aggregate-lifecycle`.
- **No direct commits to `main`** — always feature branch → PR.

### CI / Release Pipeline (GitHub Actions)
- **CI** on push/PR to `main`: restore, build (Release), Tier 1 + Tier 2 tests. Tier 3 may be optional per workflow.
- **Release** on merge to `main`: semantic-release determines version from Conventional Commits, runs tests, packs 5 NuGets, publishes to NuGet, creates GitHub Release, updates `CHANGELOG.md` automatically.
- **Never force-push to `main`** under any circumstance.
- **Never skip git hooks** (`--no-verify`) or signing (`--no-gpg-sign`) without explicit user approval.

### Pre-Commit Verification (Required)
- **Validate narrowly first**, then broaden when touching shared contracts, auth, AppHost, DAPR, projections, or public APIs. At minimum, run the affected build/test projects before committing. No green, no commit.
- For Tier 2 changes: `dapr init` must be run beforehand; expect Docker running.
- For Aspire/AppHost changes: validate with `aspire run` and Aspire CLI diagnostics before pushing.
- Production-auth and Keycloak changes require smoke/integration evidence with Keycloak enabled; `EnableKeycloak=false` is dev-mode only.

### Code Review Expectation (Reviewer-Driven Patches Are the Norm)
- **Senior code review is a mandatory pipeline stage**, not a rubber stamp. Inherited from `Hexalith.EventStore` culture: Epic 2 had a 5/5 reviewer-found patch rate.
- **Implications for agents**:
  - Story specs should budget for review-found rework as expected, not as exception.
  - "Verification" stories (audit existing code) typically uncover one design or test gap per story.
  - Reviewer-found patches are applied and re-validated before the story closes.
  - **CRITICAL findings**: verify before accepting (false-positive CRITICALs are expensive — see EventStore Epic 1 retro R1-A8 for the verification-command rule).
- **Test thoroughness rule** (Epic 2 R2-A6): Tier 2/3 tests must inspect state-store end-state. "Asserts the call returned 202" is an API smoke test, not an integration test.
- **ID validation rule** (Epic 2 R2-A7): `Ulid.TryParse` for identifiers; never `Guid.TryParse` on `messageId`/`correlationId`/`aggregateId`/`causationId`.

### Submodules
- **`Hexalith.EventStore`, `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.FrontComposer`** are root-level submodules. Initialize with `git submodule update --init` — NEVER `--recursive`.
- Treat submodules as separate roots unless the task explicitly targets them. Do not mix their code, tests, generated artifacts, or package governance into unrelated Tenants changes.
- **Modifying a submodule**: commit inside the submodule first (separate repo), push, then update the parent's submodule pointer in a follow-up commit. Submodule changes propagate to all consumers of that submodule.
- **`Hexalith.EventStore` is the framework dependency** — changes there affect Tenants, FrontComposer, and any other Hexalith repo using EventStore. Coordinate breaking changes before publishing.

### DAPR & Aspire Local Dev
- **`dapr init`** required once on a dev box before Tier 2/3 tests or AppHost runs.
- **AppHost changes require an Aspire restart** to take effect — running orchestration won't pick up topology edits until `aspire run` is re-invoked.
- **Prefer non-persistent resources** for local dev unless the task explicitly needs persistence. Persistent containers leave confusing state between runs.
- **`aspire publish`** uses `PUBLISH_TARGET=docker|k8s|aca` — don't assume one publisher is active without checking environment/configuration.

### Container Publish
- **Local archive (no registry push)**: `dotnet publish src/{Project}/{Project}.csproj -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/{name}.tar.gz`.
- **Registry push**: set `SDK_CONTAINER_REGISTRY_UNAME` and `SDK_CONTAINER_REGISTRY_PWORD` env vars; use `-p:ContainerImageTags="staging-latest;staging-$(git rev-parse HEAD)"`.
- **Never commit registry credentials** — they're env-only.

### BMAD Workflow (Story-Driven Development)
- **Planning artifacts** live in `_bmad-output/planning-artifacts/` (PRD, architecture, epics, product brief, sprint change proposals, implementation-readiness reports).
- **Implementation artifacts** live in `_bmad-output/implementation-artifacts/` (sprint status, story files like `2-3-tenant-aggregate-lifecycle.md`).
- **Both are untracked** — don't commit `_bmad-output/` to PRs.
- Update BMad artifacts when the workflow requires it, but do not mix BMad artifact churn into unrelated code PRs.
- **`_bmad/` is BMAD framework tooling** (tracked). Do not modify framework files (`_bmad/bmm/`, `_bmad/cis/`, etc.) without explicit user approval — changes affect every BMAD-using repo.
- **Story spec files** are the source of truth for an in-flight story. Implementation must satisfy all Acceptance Criteria in the story file before merge.
- **`sprint-status.yaml`** should be updated only through the relevant BMad workflow.
- **Implementation Readiness Reports** validate that PRD/architecture/epics align before a sprint starts. The latest report is in `_bmad-output/planning-artifacts/implementation-readiness-report-{date}.md`.

### Pull Request Etiquette
- **PR title**: same Conventional Commit format as the squash commit message.
- **PR body**: summary of the change, test plan checklist, link to the story file or issue.
- **Never include `bin/`, `obj/`, `nupkgs/`, `TestResults-Coverage/`, `eventstore-*.log`, `status.txt`** in the PR diff.
- **`Hexalith.EventStore` submodule pointer changes** must be called out in the PR body with the upstream commit reference.

### Secret Hygiene
- **Never commit** `.env`, credentials files, JWT private keys, container registry passwords, IdP client secrets.
- **Local dev with `EnableKeycloak=false`** uses symmetric-key JWT validation — convenient for unit tests but production-like security verification requires Keycloak enabled.
- **Dev-mode JWTs** (when Keycloak is off) must use issuer `hexalith-dev`, audience `hexalith-eventstore`, `tenants` JSON-array claim, and `permissions` such as `commands:*` (inherited convention from EventStore).

---

## Critical Don't-Miss Rules

_The highest-stakes rules from all categories above. If an agent has time for only one section, this is it._

### Security & Multi-Tenancy
- **Tenant validation MUST happen before state rehydration** in the AggregateActor pipeline (Step 2 before Step 3). Never reorder.
- **Never trust user-controllable JWT claims as identity.** Use `envelope.UserId` from JWT `sub` only. Never `name`, `email`, `display_name`.
- **Sanitize `CommandEnvelope.Extensions` at the API boundary** before they enter the pipeline. Never trust client-submitted `actor:globalAdmin` or other reserved extensions.
- **Never log event payloads, command payloads, secrets, JWT tokens, or PII** at any level.
- **Cross-tenant isolation tests** (Tier 1 + Tier 2 + Tier 2/3) are non-negotiable for NFR5 (zero leaks). Cursor tokens and error bodies must also be tested for leaks.

### Domain Correctness
- **Never throw for business rule violations.** Return `DomainResult.Rejection([new XxxRejection(...)])`. Exceptions bypass EventStore's idempotency cache and cause duplicate-command replay.
- **`AggregateId` comes from `envelope.AggregateId`**, not from the command body. Use `TenantIdentity` constants instead of adding new hard-coded platform tenant/domain/global-admin literals.
- **Every event payload must include `TenantId` as a top-level field** (envelope `TenantId` is always `"system"`).
- **Global administrators use the separate `global-administrators` domain** and singleton aggregate ID. Do not route global-admin events as normal tenant-domain events.
- **IDs are ULIDs, not GUIDs** — `Ulid.TryParse`, never `Guid.TryParse` on `messageId`/`correlationId`/`aggregateId`/`causationId`.
- **Rejection event payloads carry structured data only** — IDs, enums, counts. Never English strings (they're persisted forever).
- **Empty-tenant bootstrap exception**: `AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`. Preserve this.
- **`TenantAggregate` does NOT enforce a "≥1 owner" invariant.** Removing the last owner is allowed by design.

### Framework Wiring
- **Aggregates and projections must live in `Hexalith.Tenants.Server`** — that's the only assembly scanned by EventStore. Misplacement = silent no-registration = command timeout.
- **`GlobalAdministratorsAggregate`** — plural "Administrators." Easy to mistype.
- **Handle methods are `public static`** and discovered by signature (2-arg or 3-arg with `CommandEnvelope`). Renaming `Handle` or changing return type breaks dispatch.
- **Use `CachingProjectionActor`** for projection state; cross-tenant index path remains conditional pending Story 5.2 fan-in verification.
- **Projection writes must preserve optimistic concurrency and ETag behavior.** Do not collapse retry/merge logic to last-write-wins.
- **`Hexalith.Tenants` host must expose the `/process` domain processor route** — AggregateActor invokes it via DAPR. Missing route stalls pipeline at Step 4.
- **Bootstrap goes through the full MediatR pipeline**, never a shortcut. Multi-instance N-1 rejections at Information level are the contract.

### Package & Dependency
- **Never add `Version=` inline on `PackageReference`** — centralized management via `Directory.Packages.props` only.
- **Never bump `Hexalith.EventStore` submodule pointer** without coordinating: it ripples to every Hexalith repo.
- **Never run `git submodule update --init --recursive`** — nested submodules break the build.
- **Never install the obsolete Aspire workload** (`dotnet workload install aspire`). Use Aspire CLI + `Aspire.AppHost.Sdk`.
- **Dependency direction never reverses** toward `Contracts`. Cross-module references are same-tier (`Tenants.{Tier} → EventStore.{Tier}`).
- **Do not add direct Redis/database/broker coupling** when EventStore/DAPR already owns the infrastructure boundary.

### Test Integrity
- **Never disable the conformance test, naming-convention tests, or serialization round-trip tests** to ship a change. They are release blockers.
- **Never skip auth/isolation or projection write-safety tests** to make a story pass.
- **Use signed opaque scoped cursors**; never add offset/limit pagination or replayable cross-scope cursors.
- **Never use `Assert.*`** — Shouldly only.
- **Never write Tier 2/3 tests that assert only HTTP status or mock-call counts** — inspect state-store end-state.
- **Never `Thread.Sleep` / `Task.Delay`** to wait for async state — poll observable state with a bounded timeout.
- **Never construct `CommandEnvelope` inline** in tests — use the `CreateCommand<T>` helper.
- **Never assert on raw log message text** — use semantic logging properties.

### Build / Release
- **Never use `feat:` for a refactor** — triggers a minor version bump and NuGet publish. Use `refactor:`.
- **Never force-push to `main`**, never skip git hooks without explicit approval.
- **Run `dotnet build && dotnet test`** before every commit. No green, no commit.
- **Never `.sln`** — `Hexalith.Tenants.slnx` only.
- **Never modify `_bmad/` framework files** without explicit user approval — affects every BMAD-using repo.

### Style & Code Quality
- **K&R brace style** — opening `{` on same line. Diverges from EventStore's Allman; don't "fix."
- **Prefer existing project boundaries over new abstractions.** Add shared abstractions only when they match an established boundary or remove real duplication.
- **Controllers are adapters**: route/query validation, authenticated identity extraction, cursor validation, and dispatch. Domain logic belongs in aggregates; read logic belongs in projections/query handlers.
- **Default to `sealed`** for new types. Exceptions are aggregates/state/projections/read models (open for EventStore inheritance).
- **`System.Text.Json` only** — never Newtonsoft.Json. Use the shared `JsonSerializerOptions` factory; never inline `new JsonSerializerOptions()`.
- **`DateTimeOffset`** (not `DateTime`) for timestamps; field name convention `{Action}At`.
- **Validators reference `TenantAggregate.MaxConfigurationKeys`/`MaxKeyLength`/`MaxValueLength` constants** — never duplicate the literals.
- **XML docs not enforced** — add only when the comment carries non-trivial context, OR when matching the existing public DI extension pattern.

### Tooling
- **Use ripgrep first** — `rg` / `rg --files` are available and should be preferred for code and file searches.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing code in this repository.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Check the Type Location table for any new type before writing it.
- Check the Critical Don't-Miss section before opening a PR.
- Update this file if a new non-obvious project pattern emerges.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update when technology stack, framework patterns, or project conventions change.
- Review periodically for outdated rules (especially the version pins and preview-Aspire notes).
- Remove rules that become obvious or stop preventing real mistakes.

Last Updated: 2026-05-31
