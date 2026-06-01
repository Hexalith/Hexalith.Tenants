---
baseline_commit: 557de8d
---

# Story 4.2: Expose Consumer DI Registration for Tenant Client Services

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want to register tenant client services with one DI extension method,
so that tenant integration setup remains small and repeatable across services.

## Acceptance Criteria

1. **Given** a consuming service references the Client package
   **When** the developer calls the tenant client registration extension
   **Then** the required tenant client services are registered in `IServiceCollection`
   **And** the extension returns `IServiceCollection` for chaining.

2. **Given** the registration extension is called with default options
   **When** the consuming service starts
   **Then** the client services use documented defaults compatible with EventStore and DAPR conventions
   **And** no server-only implementation types are required by the consumer.

3. **Given** the registration extension is called with configuration options
   **When** the developer supplies valid settings
   **Then** the extension binds and validates those settings consistently with the project options pattern
   **And** invalid settings fail clearly during startup or validation.

4. **Given** a developer reviews the public API
   **When** they inspect Client package registration methods
   **Then** there is a single documented primary registration path
   **And** public extension methods include the existing project style of XML documentation.

5. **Given** client registration tests run
   **When** services are registered with default and configured options
   **Then** tests verify expected service descriptors are present
   **And** no host, AppHost, or Server-only dependency is introduced into consumer registration.

## Tasks / Subtasks

- [x] Task 1: Audit and tighten the public Client registration surface (AC: #1, #4)
  - [x] Confirm `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` remains the single primary public registration path.
  - [x] Keep `AddHexalithTenants(this IServiceCollection services)` and `AddHexalithTenants(this IServiceCollection services, Action<HexalithTenantsOptions> configureOptions)` returning the same `IServiceCollection`.
  - [x] Preserve null guards for `services` and `configureOptions`.
  - [x] Keep XML documentation on public extension methods and options types concise and useful; do not add boilerplate to every internal helper.

- [x] Task 2: Align default options with current EventStore/DAPR conventions (AC: #2, #3)
  - [x] Keep `PubSubName` default as `pubsub`.
  - [x] Keep `TopicName` default as `tenants.events`.
  - [x] Resolve the stale `CommandApiAppId = "commandapi"` default in `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs`: current AppHost uses `eventstore` as the command gateway app id and `tenants` as the Tenants domain service app id. If no registered client service consumes this option, remove or defer it rather than preserving a wrong public default.
  - [x] Add a single `ConfigurationSectionName` constant, expected value `Tenants`, so binding and docs do not duplicate the section name.
  - [x] Ensure defaults match `docs/event-contract-reference.md`, `src/Hexalith.Tenants.AppHost/Program.cs`, and Story 4.1 completion notes.

- [x] Task 3: Add explicit options validation without new unnecessary dependencies (AC: #3)
  - [x] Add validation for required non-whitespace values used by runtime registration, at minimum `PubSubName` and `TopicName`.
  - [x] If a command app id option remains public, validate it as required non-whitespace and set it to the current command gateway app id.
  - [x] Prefer an `IValidateOptions<HexalithTenantsOptions>` implementation or `OptionsBuilder.Validate(...)` using `Microsoft.Extensions.Options`; avoid adding `Microsoft.Extensions.Options.DataAnnotations` unless the codebase already needs annotations.
  - [x] Use `ValidateOnStart()` where available so invalid settings fail during host startup; tests may also prove `IOptions<HexalithTenantsOptions>.Value` throws `OptionsValidationException`.
  - [x] Keep validation error messages actionable and name the failing option keys.

- [x] Task 4: Preserve idempotent service registration and consumer dependency boundaries (AC: #1, #2, #5)
  - [x] Keep `DaprClient` registration idempotent: do not add a second `DaprClient` descriptor when the consumer already registered one.
  - [x] Keep options configuration idempotent enough that repeated `AddHexalithTenants()` calls do not stack duplicate configuration for the same default path.
  - [x] Keep existing Client infrastructure registrations only if they are consumer-facing and do not pull `Hexalith.Tenants.Server`, `Hexalith.Tenants`, or `Hexalith.Tenants.AppHost`.
  - [x] Preserve support for a consumer-provided `ITenantProjectionStore` without overwriting it.
  - [x] Do not introduce direct broker, Redis, database, or server-domain dependencies into the Client package.

- [x] Task 5: Update documentation and sample assumptions for the primary registration path (AC: #2, #4)
  - [x] Update `docs/quickstart.md` if the defaults or registered services wording changes.
  - [x] Update `README.md` only if the Client package description or setup snippet is stale.
  - [x] Update `samples/Hexalith.Tenants.Sample/Program.cs` comments only if they mention removed or renamed services.
  - [x] Keep the public sample setup small: `builder.Services.AddHexalithTenants();` must remain the first-class path.

- [x] Task 6: Expand Client registration tests (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` for default option values, configured option values, invalid option failures, return value chaining, idempotency, and consumer-provided service preservation.
  - [x] Add or update a boundary test that scans `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` and fails if it references `Hexalith.Tenants.Server`, `Hexalith.Tenants`, or `Hexalith.Tenants.AppHost`.
  - [x] Assert no inline package versions are added to the Client project file.
  - [x] Do not resolve `DaprClient` in unit tests; descriptor assertions are enough because resolving can require DAPR/gRPC infrastructure.

- [x] Task 7: Verification (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Client.Tests/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release`.
  - [x] If `dotnet test` cannot run in the sandbox because VSTest cannot open sockets, build the test project and use the direct xUnit runner fallback already documented in Story 4.1.

## Dev Notes

### Scope

This story hardens the consumer DI registration contract for `Hexalith.Tenants.Client`. It is not a new event-processing feature story. The dev agent should focus on a reliable, documented, validated one-call setup path.

Do not create a second registration API or a sample-specific helper. The main developer experience remains:

```csharp
builder.Services.AddHexalithTenants();
```

### Current State To Preserve Or Correct

- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` already exposes `AddHexalithTenants()` overloads and registers `DaprClient`, `HexalithTenantsOptions`, projection/event processing infrastructure, and the event type registry.
- `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs` currently has defaults `PubSubName = "pubsub"`, `TopicName = "tenants.events"`, and `CommandApiAppId = "commandapi"`.
- `CommandApiAppId = "commandapi"` is a stale risk. Story 4.1 notes the current AppHost uses app ids `eventstore`, `tenants`, `eventstore-admin`, and `sample`; `commandapi` was explicitly fixed out of static DAPR component scopes. Do not leave a wrong public default in the Client package.
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` already covers many happy paths. It lacks explicit invalid-options coverage and should be extended rather than replaced.
- `samples/Hexalith.Tenants.Sample/Program.cs` demonstrates the desired setup path and should continue to compile without referencing Server or AppHost projects.

### Architecture Guardrails

- Client-facing code belongs under `src/Hexalith.Tenants.Client`.
- Public contracts remain in `src/Hexalith.Tenants.Contracts`.
- Server-only aggregates, validators, projections, host wiring, and AppHost topology must not be referenced by the Client package.
- DAPR is the abstraction for pub/sub. Do not introduce Redis, Kafka, RabbitMQ, database, or broker-specific APIs.
- The shared tenant event topic is `tenants.events`; consumers filter by event type on the shared topic.
- DAPR pub/sub is at-least-once. Registration must not imply exactly-once delivery or cross-service ordering.
- Use `System.Text.Json`, `DateTimeOffset`, and existing event payload contracts. Do not introduce Newtonsoft.Json.

### Options Pattern Guidance

- Use the .NET options pattern already present in the codebase.
- `Configure<TOptions>(configuration.GetSection(...))` is acceptable for binding, but validation should be explicit.
- Microsoft guidance supports `OptionsBuilder.Validate(...)`, `ValidateOnStart()`, and `IValidateOptions<TOptions>` for validation. `IValidateOptions<TOptions>` is a good fit here because validation stays close to Client options without adding annotation packages.
- If `ValidateOnStart()` changes service descriptor counts, update tests to assert behavior rather than brittle exact counts where appropriate.

### DAPR Registration Guidance

- Endpoint subscription code uses `MapPost(...).WithTopic(options.PubSubName, options.TopicName)`. DAPR troubleshooting docs identify `MapSubscribeHandler()` plus `WithTopic(...)` endpoint registration as the path that makes `/dapr/subscribe` return subscription entries.
- This story does not need live DAPR sidecars. Unit tests should inspect service descriptors and options behavior, not make network calls.

### Previous Story Intelligence

Story 4.1 completed shared event publication on `tenants.events` for tenant and global-administrator events. Its review found and fixed a real app-id drift: static AppHost DAPR component files still referenced obsolete `commandapi` while the current topology uses `eventstore`, `tenants`, `eventstore-admin`, and `sample`.

Carry that learning into this story: defaults exposed by the Client package are part of the integration contract. A stale default can make package consumers copy broken configuration even when local tests pass.

Story 4.1 verification also documented this sandbox issue: `dotnet test` through VSTest may fail before test execution because the runner cannot open a TCP listener. Direct xUnit runner fallback was used successfully after builds completed.

### Git Intelligence

Recent commits:

- `557de8d feat(story-4.1): Publish Tenant Domain Events as CloudEvents`
- `ac3e5e0 docs(retro): sync Epic 3 retrospective`
- `9a779f0 feat(story-3.8): Reject Conflicting Concurrent Tenant Modifications`
- `8db9c41 feat(story-3.7): Enforce Tenant Configuration Limits`
- `f26cfa2 feat(story-3.6): Remove Tenant Configuration Entries`

The recent pattern is to add narrow tests around contract drift and configuration defaults, then verify with build/direct xUnit fallback when VSTest is blocked.

### Files To Inspect Or Update

- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` - primary DI extension path; preserve idempotency and chaining.
- `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs` - defaults and public option shape; resolve stale `commandapi`.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs` - consumes `PubSubName` and `TopicName`; keep option names compatible or update this file with tests.
- `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` - Client package dependencies; no Server/AppHost references and no inline versions.
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` - main registration test file; extend in place.
- `samples/Hexalith.Tenants.Sample/Program.cs` - package consumer example; keep one-call registration visible.
- `docs/quickstart.md` and `README.md` - update only for default/wording drift.

### Testing Standards

- Use xUnit v3 and Shouldly.
- Do not use `Assert.*`.
- Test methods use the existing snake_case style only if adding a new test class that already uses it; otherwise follow the surrounding file convention.
- Do not add `using Xunit;` to test files because tests inherit the global using.
- Descriptor assertions should not instantiate `DaprClient`.
- Test invalid options by resolving options or invoking startup validation; assert `OptionsValidationException` with meaningful failures.

### Project Structure Notes

- This story touches Client, Client.Tests, and possibly docs/sample only.
- It should not require changes in `Hexalith.EventStore`, `Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, or `src/Hexalith.Tenants.AppHost`.
- If implementation discovers a true topology mismatch outside Client, document it in completion notes and keep any fix narrowly scoped.
- Do not initialize nested submodules.
- Use `Hexalith.Tenants.slnx`, not `.sln`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 4.2 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` - FR43-FR45 developer experience and packaging]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Client/Subscription structure, DAPR resource names, event integration boundaries]
- [Source: `_bmad-output/project-context.md` - package boundaries, DAPR conventions, testing rules, central package management]
- [Source: `_bmad-output/implementation-artifacts/4-1-publish-tenant-domain-events-as-cloudevents.md` - shared topic completion, app-id drift review, sandbox test fallback]
- [Source: `docs/event-contract-reference.md` - shared `tenants.events` topic, at-least-once delivery, consumer filtering]
- [External: Microsoft Learn, Options pattern - https://learn.microsoft.com/en-us/dotnet/core/extensions/options]
- [External: DAPR docs, Troubleshoot Pub/Sub with the .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-troubleshooting/dotnet-troubleshooting-pubsub/]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Plain `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore` failed before compilation because sandbox permissions denied MSBuild node pipe/socket creation (`MSB1025`, `SocketException (13): Permission denied`).
- 2026-06-01: `dotnet build tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built successfully, then VSTest aborted before executing tests because its TCP listener was denied (`SocketException (13): Permission denied`).
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 57, Failed 0, Skipped 0.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Client.Tests/bin/Release/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 57, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Release regression pass completed for Sample, Client, Contracts, Integration, Server, and Testing test assemblies: Total 988, Failed 0, Skipped 25 DAPR/performance-gated integration tests.
- 2026-06-01 review: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built successfully, then VSTest aborted before executing tests because its TCP listener was denied (`SocketException (13): Permission denied`).
- 2026-06-01 review: Direct xUnit fallback `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 66, Errors 0, Failed 0, Skipped 0.
- 2026-06-01 review: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01 review: Direct xUnit fallback `tests/Hexalith.Tenants.Client.Tests/bin/Release/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 66, Errors 0, Failed 0, Skipped 0.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Validation checklist applied during story creation; current code, prior story learnings, dependency boundaries, and stale `commandapi` default risk are called out explicitly.
- Removed the unused stale `CommandApiAppId` option from the Client registration surface instead of preserving the obsolete `commandapi` default.
- Added `HexalithTenantsOptions.ConfigurationSectionName`, explicit options validation for `PubSubName` and `TopicName`, and startup validation registration without adding new package dependencies.
- Preserved the single `AddHexalithTenants()` registration path, DAPR client idempotency, event infrastructure registrations, and consumer-provided `ITenantProjectionStore` behavior.
- Expanded Client registration tests for defaults, configured values, invalid options, section-name binding, dependency boundary checks, and package-version governance.
- Updated quickstart wording to use the current `eventstore` command gateway and document Client defaults for the `Tenants` section, `pubsub`, and `tenants.events`.
- Senior review fixed the explicit options overload so caller-supplied configuration is applied after existing options configuration or default configuration binding.

### File List

- docs/quickstart.md
- _bmad-output/implementation-artifacts/4-2-expose-consumer-di-registration-for-tenant-client-services.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/story-automator/orchestration-1-20260531-113112.md
- src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs
- src/Hexalith.Tenants.Client/Configuration/ValidateHexalithTenantsOptions.cs
- src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs
- tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix.

Findings:

- HIGH fixed: `AddHexalithTenants(this IServiceCollection, Action<HexalithTenantsOptions>)` silently skipped the caller's explicit configuration delegate whenever any `IConfigureOptions<HexalithTenantsOptions>` already existed. That made configured registration order-dependent and failed AC #3 for consumers that preconfigure options or call the parameterless overload before applying explicit settings. Fixed by always registering the explicit delegate and adding regression tests for both existing manual options configuration and prior default configuration binding.
- MEDIUM fixed: Story File List did not include all changed story-automator/test-summary artifacts observed in git status. Updated the File List for review transparency.

Acceptance criteria review:

- AC #1 implemented: both overloads return the same `IServiceCollection`, register Client services, and preserve DAPR client idempotency.
- AC #2 implemented: defaults remain `Tenants`, `pubsub`, and `tenants.events`; stale `CommandApiAppId` is removed from the Client options surface; no Server/AppHost project dependency is introduced.
- AC #3 implemented after review fix: configured values bind, explicit values apply after existing configuration, and invalid values fail through options validation/startup validation.
- AC #4 implemented: `TenantServiceCollectionExtensions` remains the single primary registration path with XML documentation on public extension methods.
- AC #5 implemented: Client registration tests cover descriptors, defaults, configured values, invalid values, dependency boundaries, and package-version governance.

Validation:

- Microsoft options guidance checked for `IValidateOptions<TOptions>` and `ValidateOnStart()` behavior.
- DAPR pub/sub guidance checked for `MapSubscribeHandler()` and topic registration behavior.
- `dotnet test tests/Hexalith.Tenants.Client.Tests/` via VSTest could not execute because the sandbox denied the TCP listener, after a successful build.
- Direct xUnit Debug Client test run passed: 66 total, 0 failed, 0 skipped.
- Release solution build passed: 0 warnings, 0 errors.
- Direct xUnit Release Client test run passed: 66 total, 0 failed, 0 skipped.

### Change Log

- 2026-06-01: Hardened Client DI registration defaults, validation, docs, and tests for Story 4.2.
- 2026-06-01: Review auto-fix applied for explicit options configuration precedence and story record transparency.
