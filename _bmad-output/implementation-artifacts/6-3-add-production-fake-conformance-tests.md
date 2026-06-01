---
baseline_commit: 8a1e60e
---

# Story 6.3: Add Production/Fake Conformance Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want conformance tests that compare in-memory fakes with production aggregate behavior,
so that fake behavior remains trustworthy across every command type.

## Acceptance Criteria

1. Given tenant command contracts are available, when conformance tests discover or enumerate supported command types, then each tenant lifecycle, membership, role, configuration, and global-administration command is included in the conformance suite, and skipped command types must be explicitly justified.
2. Given a conformance command sequence is executed against production aggregate logic, when the same sequence is executed through the testing fake, then both paths produce equivalent event and rejection sequences, and final aggregate state is equivalent for the tested scope.
3. Given authorization context matters for a command, when conformance tests execute the command, then authorized, unauthorized, global-admin, missing-member, disabled-tenant, and duplicate-operation variants are covered where applicable.
4. Given a new command type is added in Contracts or Server, when conformance tests run, then the missing command is detected by the conformance coverage mechanism, and the test suite fails until the command is added to conformance coverage.
5. Given conformance tests fail, when the failure output is reviewed, then the output identifies the command sequence and differing event or rejection type, and it does not dump sensitive command payloads or secrets.
6. Given new tenant success events may be added to `Contracts.Events` over time (TEN-4 correction), when the projection-conformance test enumerates every non-rejection event payload type, then it asserts each type is explicitly handled by `InMemoryTenantProjection.Apply` and none reaches the silent `default:` arm, and adding an unwired success event fails the conformance test inside the Tenants test suite.

## Tasks / Subtasks

- [x] Audit existing conformance coverage before adding new code (AC: 1, 2, 3, 4, 6)
  - [x] Treat `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` as the current candidate implementation, not disposable legacy code.
  - [x] Treat `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs` as the current candidate projection drift guard.
  - [x] Inventory all command contracts currently under `src/Hexalith.Tenants.Contracts/Commands`: `CreateTenant`, `UpdateTenant`, `DisableTenant`, `EnableTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`, `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator`.
  - [x] Confirm every command has at least one conformance scenario that compares production aggregate behavior to `InMemoryTenantService` behavior, or add an explicit justified skip list that fails on stale entries.
- [x] Harden command coverage detection so new commands cannot slip through (AC: 1, 4)
  - [x] Replace or augment the current hard-coded count check with an explicit discovered-type-to-covered-scenario mapping.
  - [x] Ensure the test fails with a concise message naming uncovered command type names only.
  - [x] Do not depend on per-command test method names as the only coverage mechanism; make coverage data intentional and reviewable.
  - [x] Keep the command discovery scoped to Tenants command contracts and exclude non-command DTOs/queries.
- [x] Expand production/fake sequence equivalence assertions (AC: 2, 5)
  - [x] Compare result kind (`Success`, `Rejection`, `NoOp`), ordered event/rejection type sequence, and stable event fields for each scenario.
  - [x] Add final-state equivalence checks for tenant aggregate state in the tested scope: status, users, configuration, and tenant identity fields that are observable through `TenantState`.
  - [x] Add final-state equivalence checks for global-administrator state: bootstrapped flag and administrator set.
  - [x] Keep timestamp comparisons stable: compare type and non-time fields unless the scenario intentionally controls time. Do not make tests flaky by asserting exact `DateTimeOffset.UtcNow` values.
  - [x] Failure output must name the scenario and differing event/rejection type or state field; do not serialize full command payload bytes or command JSON.
- [x] Cover authorization and business-rule variants by command family (AC: 3)
  - [x] Lifecycle: global-admin success for create/disable/enable, non-global-admin rejection where required, duplicate create, not-found disable/enable, and already-active/already-disabled lifecycle no-op or rejection semantics as production defines them.
  - [x] Tenant profile: global-admin success, TenantContributor success, unauthorized rejection, missing tenant rejection, disabled tenant rejection.
  - [x] Membership: global-admin success, TenantOwner success, unauthorized rejection, missing tenant, disabled tenant, duplicate user, missing member, and role-escalation/invalid-role variants.
  - [x] Configuration: global-admin success, TenantOwner success, unauthorized rejection, missing tenant, disabled tenant, missing key, idempotent same-value no-op, key length, value length, and max-key-count variants.
  - [x] Global administration: bootstrap success, already-bootstrapped rejection, set success, set duplicate, set unauthorized, remove success, remove not-found, remove unauthorized, and remove-last-admin rejection.
  - [x] Preserve the empty-tenant first-user bootstrap behavior in `TenantAggregate`: non-global admin add-user may bypass RBAC only when `HasMembershipHistory == false`.
- [x] Harden projection conformance for success events (AC: 6)
  - [x] Ensure `InMemoryTenantProjectionConformanceTests` enumerates all `IEventPayload` types from `Contracts.Events` excluding `IRejectionEvent`.
  - [x] Ensure every non-rejection success event type is explicitly listed as handled and has a behavioral assertion proving it is routed rather than dropped into `default:`.
  - [x] Keep rejection events ignored by projection tests; success-event drift is the TEN-4 guard.
  - [x] Do not change `InMemoryTenantProjection.Apply` default behavior unless the conformance tests prove an actual product requirement gap.
- [x] Preserve package and architecture boundaries (AC: 1-6)
  - [x] Keep all new or changed conformance tests in `tests/Hexalith.Tenants.Testing.Tests`.
  - [x] Do not move aggregate/domain logic from `Server` into `Testing`.
  - [x] Do not add DAPR, Aspire, Docker, Testcontainers, Redis, HTTP host, or live EventStore requirements.
  - [x] Do not add package references or inline `Version=` attributes unless a real compile gap proves it necessary.
- [x] Validate locally (AC: 1-6)
  - [x] `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] If VSTest cannot open sockets in the local sandbox, run the built xUnit v3 in-process executable and record the fallback command and results, for example `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -class Hexalith.Tenants.Testing.Tests.Conformance.TenantConformanceTests -class Hexalith.Tenants.Testing.Tests.Projections.InMemoryTenantProjectionConformanceTests`.

## Dev Notes

- This is a hardening/reconciliation story against the current workspace state. Candidate command conformance tests already exist in `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`, and candidate projection conformance tests already exist in `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`. Start by auditing and strengthening them against Story 6.3 acceptance criteria instead of creating a parallel test suite. [Source: repository scan; `_bmad-output/implementation-artifacts/6-2-reuse-production-aggregate-logic-in-testing-fakes.md#Current-State-Notes`]
- Epic 6 goal: developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`]
- PRD FR47 is the core guarantee: fakes execute the same domain logic as production for command validation, event production, and state transitions, verified by a conformance suite that runs identical command sequences against the fake and production aggregate. Projection-level/query-level isolation remains the consuming service's responsibility. [Source: `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`]
- NFR4 remains active: in-memory testing fakes should execute commands and produce events within 10ms as measured by xUnit test execution time. Keep conformance tests Tier 1 and avoid infrastructure. [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`]
- Architecture maps Epic 6 work to `src/Hexalith.Tenants.Testing` and `tests/Hexalith.Tenants.Testing.Tests`; production/test parity belongs in `Testing.Tests/Conformance`. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`; `_bmad-output/planning-artifacts/architecture.md#Test-Organization`]
- Component boundary: `Testing` provides in-memory fakes and helpers without changing production domain behavior. `Server` remains the domain logic and EventStore-discovered aggregate/projection surface. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`]
- The current repository already includes all 12 command contract files listed in the tasks. A command added later must fail conformance coverage until it receives scenarios or an explicit justified skip. [Source: `src/Hexalith.Tenants.Contracts/Commands`]
- Current success event payloads are `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved`, `GlobalAdministratorSet`, and `GlobalAdministratorRemoved`. Projection conformance must fail when another non-rejection `IEventPayload` appears without explicit handling. [Source: `src/Hexalith.Tenants.Contracts/Events`; `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`]
- TEN-5 remains active: `InMemoryTenantService` and `TenantTestHelpers` intentionally return `Hexalith.EventStore.Contracts.Results.DomainResult`. Do not introduce a Tenants-owned result wrapper. [Source: `_bmad-output/planning-artifacts/architecture.md#Security-&-Contract-Hardening-Decisions-Correct-Course-2026-05-27`; `_bmad-output/implementation-artifacts/6-2-reuse-production-aggregate-logic-in-testing-fakes.md#Dev-Notes`]
- Security-sensitive context: global-admin authority is represented by trusted `actor:globalAdmin` envelope metadata. Conformance tests may create that metadata through `TenantTestHelpers.CreateCommandEnvelope`, but production or fake logic must not infer global-admin authority from command payloads or arbitrary user claims. [Source: `_bmad-output/project-context.md#Authorization-RBAC-Role-Based-Access-Control`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]

### Existing Code To Reuse

- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`: current conformance test suite. It already uses reflection to discover command types, compares event sequences for many success/rejection/no-op paths, and uses `[Trait("Category", "Conformance")]`. Strengthen coverage mapping, final-state equivalence, and failure diagnostics rather than replacing it wholesale.
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`: current TEN-4 projection drift guard. It already enumerates success event names and asserts tenant-scoped events are routed. Ensure it remains explicit and complete as event contracts evolve.
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`: fake command processor that delegates to `TenantAggregate.Handle(...)` and `GlobalAdministratorsAggregate.Handle(...)`, applies successful events through production state `Apply` methods, and records successful `EventHistory`.
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`: in-memory read model projection. It routes known success events to server read models, ignores `IRejectionEvent`, and currently has a silent `default:` arm guarded by conformance tests.
- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`: helper for short setup and explicit command-envelope creation. Use it for aggregate ID, actor user ID, and global-admin metadata.
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`: production tenant aggregate. It derives the effective tenant ID from `CommandEnvelope.AggregateId`; conformance scenarios must use identical envelopes for production and fake paths.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`: production tenant state and `Apply` methods. Use this for direct aggregate path final-state comparison.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs` and `GlobalAdministratorsState.cs`: production global-admin behavior and state.

### Previous Story Intelligence

- Story 6.2 completed at commit `8a1e60e` and verified that the fake delegates to production aggregate `Handle` methods and production state `Apply` methods. Story 6.3 should assume that baseline but prove it continuously through exhaustive conformance.
- Story 6.2 added `tests/Hexalith.Tenants.Testing.Tests/Fakes/Story62ProductionAggregateParityTests.cs` for focused parity. Do not merge those focused tests into 6.3 unless it clearly reduces duplication while preserving signal.
- Story 6.2 deliberately left broad every-command conformance and projection drift coverage to this story. Keep 6.3 scoped to tests and small test-support adjustments unless conformance exposes a real fake/projection defect.
- Previous local validation hit VSTest sandbox socket failures, then passed through the xUnit v3 in-process executable. Record the same fallback if it happens again.
- The current worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-5-20260601-061130.md`. Ignore it unless the user explicitly asks to reconcile story-automator artifacts.

### File-Specific Guardrails

- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
  - Current state: broad candidate conformance suite with reflection discovery and many per-command success/rejection scenarios.
  - This story changes: make coverage mapping intentional, cover missing variants, add final-state equivalence, and improve failure diagnostics.
  - Preserve: xUnit v3, Shouldly, `[Trait("Category", "Conformance")]`, no infrastructure, no sensitive payload dumps.
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`
  - Current state: TEN-4 success-event drift guard.
  - This story changes: ensure every current and future non-rejection success event is explicitly handled and behaviorally routed.
  - Preserve: rejection events ignored, no DAPR/Aspire dependencies.
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - Current state: stateful wrapper around production aggregate/state methods.
  - This story changes: normally none; change only if conformance proves fake behavior diverges from production aggregate behavior.
  - Preserve: `DomainResult` public result type, successful-event-only `EventHistory`, no duplicated domain rules.
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
  - Current state: explicit known-event routing plus silent `default:` arm guarded by tests.
  - This story changes: normally none; add explicit event handling only if a success event is actually unwired.
  - Preserve: no infrastructure dependency and read-model reuse from `Server`.

### Testing Standards

- Tier 1 only. Tests must run without DAPR, Aspire, Docker, actors, Redis, broker, HTTP host, or a live EventStore process. [Source: `_bmad-output/project-context.md#Three-Tier-Test-Model`]
- Use xUnit v3 attributes and Shouldly assertions. Do not use `Assert.*`; every test must contain at least one Shouldly assertion. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Test class names use plural `{TypeUnderTest}Tests.cs`; test method names use `snake_case_with_PascalCase_for_type_names` for new tests. Existing conformance method names may be normalized if touched, but do not churn unrelated names. [Source: `_bmad-output/project-context.md#Test-Naming-Layout`]
- Do not skip or disable conformance tests to make the suite pass. Conformance is a release blocker. [Source: `_bmad-output/project-context.md#Mandatory-Test-Categories`]
- Use `TenantTestHelpers.CreateCommandEnvelope<T>` for explicit aggregate ID, actor user ID, and global-admin metadata setup. Do not construct ad-hoc envelopes unless a scenario needs to prove helper behavior. [Source: `_bmad-output/project-context.md#CommandEnvelope-Test-Helper`]
- Keep package versions centralized. Do not add inline `Version=` attributes to `PackageReference`. [Source: `_bmad-output/project-context.md#Package-Management`]

### Git Intelligence

- Recent commit `8a1e60e feat(story-6.2): Reuse Production Aggregate Logic in Testing Fakes` is the immediate baseline for this story.
- Recent commit `23990aa feat(story-6.1): Provide In-Memory Tenant Test Fakes` introduced the in-memory fake and helper baseline.
- Older Epic 3 and Epic 5 work updated `TenantAggregate`, role/configuration behavior, projection behavior, and conformance tests. Do not regress membership, disabled-tenant, duplicate operation, configuration limit, or query/projection assumptions while hardening 6.3.

### Latest Technical Notes

- No dependency upgrade is required for this story. Use the repository-pinned .NET 10 SDK, xUnit v3, Shouldly, and existing EventStore/Tenants project references.
- External package/API research is intentionally unnecessary here because the story is about local production aggregate/fake parity and existing test-framework usage. Adding or upgrading packages would increase risk without satisfying an acceptance criterion.

### Project Structure Notes

- Expected files to review/update:
  - `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
  - `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`
  - `tests/Hexalith.Tenants.Testing.Tests/Fakes/Story62ProductionAggregateParityTests.cs` only for context or deduplication if useful
  - `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs` only if conformance exposes a divergence
  - `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs` only if projection conformance exposes an unwired success event
- Do not create new projects.
- Do not add host, AppHost, DAPR component, Aspire, Testcontainers, or integration-test changes for this story.
- Do not touch archived legacy story files; they are historical context only.

### References

- `_bmad-output/planning-artifacts/epics.md#Story-6.3-Add-Production-Fake-Conformance-Tests`
- `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`
- `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`
- `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`
- `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`
- `_bmad-output/planning-artifacts/architecture.md#Test-Organization`
- `_bmad-output/project-context.md#Testing-Rules`
- `_bmad-output/project-context.md#Mandatory-Test-Categories`
- `_bmad-output/implementation-artifacts/6-2-reuse-production-aggregate-logic-in-testing-fakes.md#Dev-Notes`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01: Fallback `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -class Hexalith.Tenants.Testing.Tests.Conformance.TenantConformanceTests -class Hexalith.Tenants.Testing.Tests.Projections.InMemoryTenantProjectionConformanceTests` passed: Total 122, Failed 0.
- 2026-06-01: Required build `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Full Testing.Tests fallback `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor` passed: Total 175, Failed 0.
- 2026-06-01: Additional Tier 1 fallback executables passed: Contracts.Tests Total 104, Client.Tests Total 92, Sample.Tests Total 31.
- 2026-06-01 review: MCP documentation search attempted through Aspire MCP for checklist compliance, but the tool call was cancelled by the MCP server/client; no external API guidance was needed because the story only changes local xUnit/Shouldly conformance tests.
- 2026-06-01 review: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` passed with 0 warnings and 0 errors after review fixes.
- 2026-06-01 review: `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` rebuilt successfully, then VSTest aborted with the known `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01 review: Fallback `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -class Hexalith.Tenants.Testing.Tests.Conformance.TenantConformanceTests -class Hexalith.Tenants.Testing.Tests.Projections.InMemoryTenantProjectionConformanceTests` passed: Total 123, Failed 0.
- 2026-06-01 review: Full Testing.Tests fallback `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor` passed: Total 176, Failed 0.
- 2026-06-01 review: Additional Tier 1 fallback executables passed: Contracts.Tests Total 104, Client.Tests Total 92, Sample.Tests Total 31.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 6.3 created as a conformance hardening story against the current workspace, with explicit guardrails to audit existing command/projection conformance tests rather than recreate the surface.
- Validation checklist applied during story creation; no blocking gaps remain for dev-story handoff.
- Added intentional command coverage mapping that discovers the 12 tenant command contract types and fails with uncovered/stale command names only.
- Added scenario-driven production/fake parity coverage for tenant lifecycle, tenant profile, membership, configuration, and global-administration command families, including authorization, duplicate, missing, disabled, invalid-role, no-op, and first-user bootstrap variants.
- Added sequence-level assertions for result kind, ordered event/rejection type names, stable non-time payload fields, and final tenant/global-administrator state equivalence.
- Hardened projection drift tests so every explicitly handled non-rejection success event also has a behavioral routing assertion.
- Review fixed conformance failure diagnostics so stable-field mismatches do not dump compared command payload values or secrets; failures name the scenario, event/rejection type, and field.
- Review added payload/envelope tenant identity coverage and observed-tenant tracking so unexpected fake state under a command payload tenant ID is detected.

### Change Log

- 2026-06-01: Hardened production/fake conformance coverage and projection success-event drift guards for Story 6.3.
- 2026-06-01: Senior review auto-fixed safe failure diagnostics and envelope/payload identity state coverage; story approved and marked done.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix. No critical issues remain.

Findings fixed:

- [HIGH] Conformance result and state comparisons could include actual stable payload values in Shouldly failure output, which violated AC5's requirement to identify the differing sequence/type/field without dumping sensitive payloads. Fixed by comparing sensitive fields and dictionaries through boolean equality assertions with field-only messages.
- [MEDIUM] Final-state equivalence only checked aggregate IDs tracked by the production path, so a fake implementation that also created state under a command payload tenant ID could escape Story 6.3's state comparison. Fixed by tracking both envelope aggregate IDs and payload tenant IDs and adding an envelope/payload identity scenario.
- [LOW] Projection conformance global-administrator assertions used collection contains assertions that could echo user IDs in failure details. Fixed by using boolean routing assertions with safe messages.

Checklist validation:

- Story status was reviewable before review and is now done.
- Acceptance criteria and completed tasks were cross-checked against the changed conformance/projection tests.
- File List matches the source files changed for this story; unrelated orchestration artifact remains ignored per story notes.
- Security review focused on AC5 failure-output safety and reserved envelope aggregate identity behavior.
- Build and fallback xUnit validation passed; VSTest socket failure remains an environment limitation already documented.

### File List

- _bmad-output/implementation-artifacts/6-3-add-production-fake-conformance-tests.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs
- tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs
