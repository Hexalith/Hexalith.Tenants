---
baseline_commit: f26cfa237b0a82fed7660fcc02dbf6945d185d22
---

# Story 3.7: Enforce Tenant Configuration Limits

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want configuration limit violations to be rejected clearly,
so that tenant settings remain bounded and safe for event storage and consumers.

## Acceptance Criteria

1. Given a tenant has fewer than the maximum allowed configuration keys, when a `TenantOwner` or trusted global-admin actor adds a valid new key, then the configuration set command succeeds and the aggregate state remains within the maximum 100-key limit.
2. Given a tenant already has the maximum allowed number of configuration keys, when a `TenantOwner` or trusted global-admin actor attempts to add another distinct key, then the aggregate returns a structured configuration-key-limit rejection and the rejection identifies the limit and current usage with structured fields.
3. Given a configuration key exceeds 256 characters or violates required key validation, when the command is validated or handled, then the command is rejected with a structured key-length or key-format rejection and no configuration event is produced.
4. Given a configuration value exceeds the maximum 1KB value length, when the command is validated or handled, then the command is rejected with a structured value-length rejection and the rejection identifies the configured limit without storing the oversized value.
5. Given configuration limit tests run, when boundary values for key count, key length, value length, empty keys, namespaced keys, and update-existing-key cases are exercised, then tests prove limits are enforced at the correct boundary and persisted rejection payloads contain structured data only.

## Tasks / Subtasks

- [x] Verify the current configuration-limit surface before editing (AC: 1-5)
  - [x] Confirm `SetTenantConfiguration` and `TenantConfigurationSet` remain the command and success event contracts; do not add duplicate set-configuration contracts.
  - [x] Confirm `ConfigurationLimitExceededRejection` is the existing structured rejection contract and decide whether it already satisfies all Story 3.7 fields before creating any additional rejection type.
  - [x] Confirm `TenantAggregate.MaxConfigurationKeys`, `MaxKeyLength`, and `MaxValueLength` remain the single source of truth for limits.
  - [x] Confirm `SetTenantConfigurationValidator` references aggregate constants and is registered through `TenantSubmitCommandValidator`.
  - [x] Read all likely UPDATE files before changing behavior: `TenantAggregate`, `TenantState`, `SetTenantConfigurationValidator`, aggregate tests, submit-command validator tests, conformance tests, integration ProblemDetails tests, and `docs/event-contract-reference.md`.

- [x] Harden aggregate limit behavior without widening scope (AC: 1-4)
  - [x] Keep `TenantAggregate.Handle(SetTenantConfiguration, TenantState?, CommandEnvelope)` as a pure static handler with no async, I/O, DAPR calls, service dependencies, or thrown business-rule exceptions.
  - [x] Preserve branch ordering: null state -> disabled state -> owner/global-admin authorization -> key/value/key-count limits -> same-value `NoOp` -> success event.
  - [x] Use `CommandEnvelope.AggregateId` as the managed tenant ID source of truth in success and rejection payloads; never use `command.TenantId` when the body and envelope diverge.
  - [x] Preserve the existing interpretation of 1KB as `1024` `string.Length` characters, not UTF-8 bytes.
  - [x] Enforce key count only for distinct new keys; updating an existing key at 100 keys must still succeed when the submitted value changes.
  - [x] Preserve same-key/same-value idempotency as `DomainResult.NoOp()` with no new rejection or duplicate `TenantConfigurationSet` event.
  - [x] Preserve exact key text for accepted namespaced keys; do not trim, normalize casing, split namespaces, or reinterpret dot-delimited keys.

- [x] Resolve key-format semantics explicitly (AC: 3, 5)
  - [x] Treat null and empty keys as validator failures, not aggregate business-rule rejections, unless the command pipeline cannot reliably prevent them.
  - [x] Reconcile current whitespace-only key behavior with Story 3.7's "required key validation" language: either keep it intentionally allowed with explicit tests/docs, or change validation to reject whitespace-only keys with focused tests and documentation.
  - [x] If a new key-format rejection is introduced, keep it structured-only, add replay-only `TenantState.Apply(...)` if required, update reflection/serialization tests, and update ProblemDetails expectations.
  - [x] Do not introduce regex-heavy namespace validation unless a planning artifact or existing test establishes a concrete allowed-character contract.

- [x] Verify rejection payload and HTTP boundary behavior (AC: 2-5)
  - [x] For key count, assert `LimitType == "KeyCount"`, `CurrentCount == 100`, and `MaxAllowed == 100`.
  - [x] For key length, assert `LimitType == "KeyLength"`, current usage equals the submitted key length, and `MaxAllowed == 256`.
  - [x] For value length, assert `LimitType == "ValueSize"`, current usage equals the submitted value length, and `MaxAllowed == 1024`; do not include the oversized value in the rejection.
  - [x] Ensure rejection contracts remain structured data only: no prose `Message`, `Reason`, serialized command payload, tokens, stack traces, local paths, or sensitive values.
  - [x] Keep `ConfigurationLimitExceededRejection` mapped to 422 ProblemDetails and included in the all-rejection catalog expectation.

- [x] Preserve authorization, disabled-state, fake, and replay behavior (AC: 1-5)
  - [x] Require `TenantOwner` for non-global-admin actors; `TenantContributor`, `TenantReader`, and non-members must receive `InsufficientPermissionsRejection` before limit details are exposed.
  - [x] Preserve trusted global-admin bypass only through server-populated `CommandEnvelope.Extensions["actor:globalAdmin"] == "true"` with exact ordinal comparison.
  - [x] Ensure disabled tenants reject before limit validation and authorization-result leakage.
  - [x] Keep `TenantState.Apply(TenantConfigurationSet)` as exact-key dictionary mutation and do not add validation inside `Apply`.
  - [x] Keep `InMemoryTenantService` delegating to `TenantAggregate.Handle`; do not reimplement limit logic in the fake.

- [x] Add or tighten focused tests (AC: 1-5)
  - [x] Aggregate tests cover 99 keys plus a distinct valid new key succeeding and resulting state staying at 100 keys.
  - [x] Aggregate tests cover the 101st distinct key rejection with structured limit fields and no success event.
  - [x] Aggregate tests cover update-existing-key success when the tenant already has 100 keys.
  - [x] Aggregate tests cover same-key/same-value `NoOp` when the tenant already has 100 keys.
  - [x] Aggregate and validator tests cover key length 256 success and 257 rejection/failure.
  - [x] Aggregate and validator tests cover value length 1024 success and 1025 rejection/failure.
  - [x] Validator and submit-command validator tests cover null key, empty key, whitespace-only key according to the resolved key-format decision, null value, empty value, and oversized value.
  - [x] Tests prove unauthorized readers/contributors/non-members do not receive limit details.
  - [x] Conformance tests prove production aggregate and `InMemoryTenantService` produce identical limit rejection sequences.
  - [x] Contract serialization/reflection tests cover any new or changed rejection shape.

- [x] Update documentation only where behavior is stale (AC: 2-5)
  - [x] Update `docs/event-contract-reference.md` if it omits exact limit values, limit-type names, or key-format behavior.
  - [x] Keep docs focused on `SetTenantConfiguration`; do not expand into query, UI, projection durability, or concurrency documentation.
  - [x] If whitespace-only keys remain accepted, document that as an intentional current validation contract or leave a dev note if docs should avoid endorsing it publicly.

- [x] Run focused validation (AC: 1-5)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~SetTenantConfigurationValidatorTests|FullyQualifiedName~TenantSubmitCommandValidatorTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution plus Release build evidence as fallback validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.7 is specifically the exhaustive configuration-limit slice. It should not rework basic set behavior, remove behavior, optimistic concurrency, query endpoints, projection durability, or Phase 2 UI. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.7: Enforce Tenant Configuration Limits`]
- PRD FR19-FR24 define configuration as low-frequency schema-free key-value settings with dot-delimited namespace conventions, set/remove events, and limits of 100 keys per tenant, 1KB per value, and 256 characters per key. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Configuration`]
- FR24 requires rejections for limit violations to identify which limit was exceeded and the current usage. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Configuration`]
- PRD FR31-FR34 require tenant roles to stay tenant-scoped: `TenantOwner` can manage configuration, while `TenantReader` and `TenantContributor` cannot manage Tenants configuration. [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- NFR8 requires disabled tenants to reject commands immediately inside the aggregate, and NFR10 requires branch coverage for tenant isolation and role authorization logic. [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- FR49 and the API architecture require corrective, deterministic rejection responses at the HTTP boundary while persisted rejection events remain structured and sanitized. [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`; `_bmad-output/project-context.md#API Surface`]

### Current Repository State

- `SetTenantConfiguration` already exists as the command record and `TenantConfigurationSet` already exists as the success event. Do not create replacements. [Source: `src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs`; `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationSet.cs`]
- `ConfigurationLimitExceededRejection(string TenantId, string LimitType, int CurrentCount, int MaxAllowed)` already exists in Contracts. Current tests expect it to serialize through reflection-driven coverage. Prefer using this existing contract unless Story 3.7 cannot be satisfied without a new key-format-specific rejection. [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationLimitExceededRejection.cs`; `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`]
- `TenantAggregate` already defines `MaxConfigurationKeys = 100`, `MaxKeyLength = 256`, and `MaxValueLength = 1024`, with a comment that 1KB is interpreted as 1024 `string.Length` characters, not bytes. Preserve that interpretation unless tests/docs are deliberately updated together. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `_bmad-output/project-context.md#Configuration Limits (TenantAggregate)`]
- `TenantAggregate.Handle(SetTenantConfiguration, TenantState?, CommandEnvelope)` already derives the managed tenant ID from `envelope.AggregateId`, checks null/disabled state before RBAC and limits, enforces owner/global-admin authorization, applies key length, value length, and key-count limit checks, treats same key/value as `NoOp`, and emits `TenantConfigurationSet` for changes. Read and preserve this shape while closing gaps. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Current aggregate tests already cover set success, update, namespaced key acceptance, null/disabled tenant, reader/contributor/non-member rejection, global-admin success, envelope aggregate-id source of truth, key length 257 rejection, value length 1025 rejection, 101st key rejection, update-at-100 success, same-value `NoOp`, key length 256 success, and value length 1024 success. Add focused missing coverage rather than rewriting the test class. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- `SetTenantConfigurationValidator` currently rejects null/empty keys, keys longer than 256, null values, and values longer than 1024. It currently allows whitespace-only keys and empty values. Story 3.7 must make whitespace-key semantics explicit. [Source: `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs`; `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs`]
- `TenantSubmitCommandValidatorTests` currently mirrors direct validator behavior for set configuration, including whitespace-only key acceptance. If whitespace semantics change, update both validator layers together. [Source: `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`]
- `TenantState.Apply(ConfigurationLimitExceededRejection)` is replay-only and must not mutate tenant configuration. `TenantState.Apply(TenantConfigurationSet)` mutates the exact key in the dictionary. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- `InMemoryTenantService.ProcessTenantCommand` delegates `SetTenantConfiguration` to `TenantAggregate.Handle`; keep fake behavior aligned through delegation and conformance tests. [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`; `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- `CommandApiRuntimeIntegrationTests` already includes `ConfigurationLimitExceededRejection` in the explicit ProblemDetails expectation list and asserts all Tenants rejection types are represented. Update this catalog only if a new rejection type is introduced. [Source: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- `docs/event-contract-reference.md` lists `ConfigurationLimitExceededRejection` for `SetTenantConfiguration` and documents the rejection fields, but it may not be explicit enough about the limit-type values and whitespace-key behavior. [Source: `docs/event-contract-reference.md#SetTenantConfiguration`; `docs/event-contract-reference.md#Quick Reference`]

### Previous Story Intelligence

- Story 3.1 established `TenantRole.Unknown = 0`, string enum serialization, trusted `actor:globalAdmin` envelope metadata, disabled-tenant branch precedence, and first-user bootstrap for `AddUserToTenant` only. Preserve these decisions. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- Story 3.2 established that tenant-scoped event/rejection payloads should derive tenant identity from `CommandEnvelope.AggregateId` when command body and envelope diverge. Keep this rule for all Story 3.7 success and rejection payloads. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`]
- Story 3.4 hardened `SetTenantConfiguration` to emit payload tenant IDs from `CommandEnvelope.AggregateId` and tightened global-admin bypass to exact `actor:globalAdmin=true`. Do not regress this while touching limit logic. [Source: `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md#Completion Notes List`; `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md#Senior Developer Review (AI)`]
- Story 3.5 completed configuration-set hardening as a verification slice. It intentionally left exhaustive limit-boundary enforcement to Story 3.7 and documented current whitespace-key behavior as intentional at that time. [Source: `_bmad-output/implementation-artifacts/3-5-set-tenant-configuration-entries.md`]
- Story 3.6 added `ConfigurationKeyNotFoundRejection` and changed removal missing-key behavior. Do not mix removal semantics into Story 3.7 except to preserve shared configuration state and exact key handling. [Source: `_bmad-output/implementation-artifacts/3-6-remove-tenant-configuration-entries.md`]
- Recent commits show current Epic 3 implementation order: `47bb606 feat(story-3.4)`, `fec602a feat(story-3.5)`, and `f26cfa2 feat(story-3.6)`. [Source: `git log --oneline -5`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, central package management, DAPR `1.17.9`, FluentValidation `12.1.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. Do not bump SDK or packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Aggregate `Handle` methods must remain pure static functions: no async, no I/O, no DAPR calls, no service dependencies, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are discovered from the `Hexalith.Tenants.Server` assembly by `Handle` signature. Do not move `TenantAggregate`, rename `Handle`, or alter dispatch-compatible signatures. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC for Tenants' own commands belongs inside aggregate handlers. EventStore `IRbacValidator`/`ITenantValidator` interfaces are for consuming services and API-gate validation; moving Tenants domain RBAC there creates a circular dependency. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Every tenant event and rejection payload must include top-level managed `TenantId` because the EventStore envelope tenant is `system`. [Source: `_bmad-output/project-context.md#Identity Scheme`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- Tests use xUnit v3 plus Shouldly, global `using Xunit`, K&R brace style, and snake_case test method names. Do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`]
- New public rejection contracts are additive public package surface. Keep records structured, update reflection/serialization tests, and verify ProblemDetails mapping if a non-default status is required. [Source: `_bmad-output/project-context.md#Per-Change Checklist (Adding a New Public Type)`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` - only for real gaps in limit branch order, payload fields, or key-format aggregate fallback.
- `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs` - likely if whitespace-only key behavior is changed or documented through tests.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` - only if a new rejection contract is introduced and replay needs an `Apply` method.
- `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationLimitExceededRejection.cs` - avoid changing shape unless required; changing public contract shape is breaking.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` - primary focused boundary and branch-order coverage.
- `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs` - direct validator boundary and key-format coverage.
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs` - submit-command payload validation parity.
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` - production/fake parity for limit rejections.
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs` and `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` - likely already cover existing rejection; update only for new/changed public contracts.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - update only if a new rejection participates in the ProblemDetails catalog or if limit ProblemDetails evidence is missing.
- `docs/event-contract-reference.md` - update exact limit values, limit type names, and key-format behavior if stale.

### Out of Scope

- Basic configuration set success behavior already owned by Story 3.5.
- Configuration removal and missing-key rejection behavior already owned by Story 3.6.
- Optimistic concurrency behavior; Story 3.8 owns conflicting concurrent modifications.
- Query endpoints, cursor pagination, projection durability, tenant audit query behavior, or Phase 2 UI/FrontComposer implementation.
- New tenant roles, custom/extensible roles, or adding `GlobalAdministrator` to `TenantRole`.
- Package, SDK, DAPR, Aspire, EventStore, submodule, or CI workflow changes.

### Latest Technical Context

- No external package or API upgrade is required. Use the pinned local stack and existing EventStore/DAPR/Aspire APIs. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- The current technical risk is local contract precision: whether `ConfigurationLimitExceededRejection` plus validator errors fully satisfy "key-length, key-format, value-length, key-count" evidence without introducing redundant rejection types.
- Keep the value-size definition stable as 1024 `string.Length` characters. Switching to UTF-8 byte counts would change existing semantics and requires coordinated tests/docs, not an incidental Story 3.7 edit. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `_bmad-output/project-context.md#Configuration Limits (TenantAggregate)`]

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.7: Enforce Tenant Configuration Limits`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Configuration`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements Traceability Matrix`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#Configuration Limits (TenantAggregate)`]
- [Source: `_bmad-output/implementation-artifacts/3-5-set-tenant-configuration-entries.md`]
- [Source: `_bmad-output/implementation-artifacts/3-6-remove-tenant-configuration-entries.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- [Source: `docs/event-contract-reference.md#SetTenantConfiguration`]

## Project Structure Notes

- Alignment: Story 3.7 stays in Contracts/Server aggregate/validator/test, Testing parity, Integration catalog, and event-contract docs areas. It should not add projects, package dependencies, controllers, query endpoints, or UI files.
- Detected variance: the repository already contains most limit implementation and tests from earlier historical work. Treat this as a verification/hardening story: close real gaps, make key-format semantics explicit, and preserve existing correct behavior.
- Keep additions localized. The highest-risk changes are public rejection contract shape changes and broad key-format validation changes; only do either with focused evidence and matching docs/tests.
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md` is automation run-state bookkeeping, not application source. Do not review it for product behavior.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.7, PRD FR19-FR24/FR31-FR34/FR49, NFR8/NFR10, architecture requirements mapping, project context, and previous Stories 3.5-3.6.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `SetTenantConfigurationValidator`, aggregate tests, submit-command validator tests, conformance tests, integration ProblemDetails tests, `InMemoryTenantService`, contracts serialization tests, and event contract docs.
- Previous-story intelligence incorporated trusted global-admin envelope metadata, disabled-state branch precedence, envelope aggregate-id consistency, exact global-admin extension comparison, current Story 3.5 whitespace-key notes, and Story 3.6 configuration removal boundaries.
- Disaster-prevention guardrails included: do not duplicate existing set contracts, do not move RBAC out of the aggregate, do not trust command-body tenant IDs over envelope aggregate IDs, do not change value-size semantics from characters to bytes, do not expose oversized values in rejection payloads, do not reimplement fake behavior separately from production aggregate logic, and do not expand into removal/concurrency/query/UI scope.
- Latest technical context reviewed from local pins and source. Review web fallback checked Microsoft Learn for ASP.NET Core exception handling/ProblemDetails and .NET `String.Length`; no external package-version change is needed for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test` validation commands were attempted as written and consistently failed after build with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Used the story-approved fallback: direct xUnit assembly execution plus Release solution build evidence.

### Implementation Plan

- Preserve the existing `SetTenantConfiguration`, `TenantConfigurationSet`, and `ConfigurationLimitExceededRejection` contracts.
- Treat whitespace-only configuration keys as intentionally accepted for backward compatibility; document and test that accepted key text is preserved exactly.
- Add focused tests for missing boundaries and leakage prevention without changing aggregate branch order or public rejection shape.
- Update event-contract documentation with exact limit values, limit type names, and structured rejection payload behavior.

### Completion Notes List

- Existing aggregate behavior already enforced key count, key length, value length, owner/global-admin authorization, disabled-state precedence, same-value `NoOp`, and envelope aggregate-id payload sourcing.
- Added aggregate coverage for 99-to-100 key success, same-value `NoOp` at the 100-key limit, whitespace-only key preservation, and authorization-before-limit behavior for reader/contributor/non-member actors.
- Added validator and submit-command validator boundary coverage for max key/value lengths, oversized key/value failures, empty values, and current whitespace-key semantics.
- Added a contract serialization/reflection guard proving `ConfigurationLimitExceededRejection` contains only structured limit fields.
- Added runtime API coverage for `/process` structured limit rejections and `/api/v1/commands` payload validation before command routing.
- Registered the EventStore validation behavior and validation exception handler in the Tenants host so typed `SetTenantConfiguration` validator failures return RFC 7807 `400` responses.
- Updated event contract documentation with `KeyCount`, `KeyLength`, and `ValueSize` details and the current key-format contract.
- No new command, success event, rejection event, dependency, controller, query, projection, or UI surface was introduced.

### File List

- `_bmad-output/implementation-artifacts/3-7-enforce-tenant-configuration-limits.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants/Program.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs`

### Change Log

- 2026-06-01: Added Story 3.7 limit-boundary, key-format, structured rejection, and authorization-leakage test coverage; documented exact SetTenantConfiguration limits and current whitespace-key behavior; moved story to review.
- 2026-06-01: Senior Developer Review validated Story 3.7 implementation, fixed Dev Agent Record file-list/completion-note gaps, synced sprint status, and marked story done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix.

### Findings

- Medium: The story File List omitted changed implementation and validation files: `src/Hexalith.Tenants/Program.cs`, `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`, `_bmad-output/implementation-artifacts/tests/test-summary.md`, and the story-automator orchestration bookkeeping file. Fixed by updating the File List.
- Medium: Completion notes did not mention the runtime validation pipeline fix or the new `/process` and `/api/v1/commands` runtime API coverage, making the story record understate the changed behavior. Fixed by updating Completion Notes.
- Low: The discovery notes said no web research was needed, but the review checklist requires MCP doc search or web fallback evidence. Fixed by recording Microsoft Learn fallback references for ASP.NET Core exception handling/ProblemDetails and .NET `String.Length`.

### Validation

- Web fallback references captured: Microsoft Learn ASP.NET Core error handling / ProblemDetails, and Microsoft Learn `System.String.Length`.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` - passed with 0 warnings and 0 errors.
- Direct xUnit fallback: focused Contracts `ConfigurationLimitExceeded` tests passed.
- Direct xUnit fallback: focused Server `TenantAggregateTests`, `SetTenantConfigurationValidatorTests`, and `TenantSubmitCommandValidatorTests` passed.
- Direct xUnit fallback: focused Testing `SetTenantConfiguration` conformance tests passed.
- Direct xUnit fallback: `CommandApiRuntimeIntegrationTests` passed.
