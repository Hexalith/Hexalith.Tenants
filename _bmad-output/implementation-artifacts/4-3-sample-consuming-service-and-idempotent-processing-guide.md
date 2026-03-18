# Story 4.3: Sample Consuming Service & Idempotent Processing Guide

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer evaluating Hexalith.Tenants,
I want a complete sample consuming service and documentation on idempotent event processing,
So that I have a proven reference implementation to follow when integrating tenant events into my own services.

## Acceptance Criteria

1. **Given** the `samples/Hexalith.Tenants.Sample` project exists
   **When** a developer inspects the sample
   **Then** it demonstrates: DI registration via `AddHexalithTenants()`, DAPR pub/sub event subscription via `MapTenantEventSubscription()`, a local projection of tenant-user-role state, and access enforcement based on the projection

2. **Given** the sample consuming service is running with DAPR sidecar
   **When** a UserAddedToTenant event is published by the tenant service
   **Then** the sample service logs the event and updates its local projection

3. **Given** the sample consuming service is running
   **When** a UserRemovedFromTenant event is published
   **Then** the sample service revokes access and logs the revocation

4. **Given** the sample project
   **When** `samples/Hexalith.Tenants.Sample.Tests` are executed
   **Then** Tier 1 tests verify the sample's event handling and projection logic

5. **Given** the project documentation
   **When** a developer reads the idempotent event processing guidance (FR42)
   **Then** it includes: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample

## Tasks / Subtasks

- [x] Task 1: Implement sample consuming service Program.cs (AC: #1, #2, #3)
    - [x] 1.1: Update `samples/Hexalith.Tenants.Sample/Program.cs` with full DI setup: `AddHexalithTenants()`, `UseCloudEvents()`, `MapSubscribeHandler()`, `MapTenantEventSubscription()`
    - [x] 1.2: Add logging configuration to show event processing in console output
    - [x] 1.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create sample access-enforcement endpoint (AC: #1)
    - [x] 2.1: Create `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs` — minimal API demonstrating projection-based access enforcement
    - [x] 2.2: Implement `GET /access/{tenantId}/{userId}` that queries `ITenantProjectionStore` and returns access status (role, membership, tenant status)
    - [x] 2.3: Verify solution builds

- [x] Task 3: Create sample custom event handler (AC: #1, #2, #3)
    - [x] 3.1: Create `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs` — implements `ITenantEventHandler<T>` for key events, logs each event to demonstrate extensibility
    - [x] 3.2: Register handler in DI alongside built-in projection handler (demonstrating multiple handlers per event type)
    - [x] 3.3: Verify solution builds

- [x] Task 4: Update Sample.csproj if needed (AC: #1)
    - [x] 4.1: Verify `Hexalith.Tenants.Sample.csproj` has all required references (Client, Contracts already present)
    - [x] 4.2: Add `Dapr.AspNetCore` package reference if not already present (needed for `UseCloudEvents()` and `MapSubscribeHandler()`)
    - [x] 4.3: Verify solution builds

- [x] Task 5: Create Tier 1 unit tests for the sample (AC: #4)
    - [x] 5.1: Create `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs` — test access check logic against in-memory projection
    - [x] 5.2: Create `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs` — test logging handler receives events
    - [x] 5.3: Keep existing `ScaffoldingSmokeTests.cs` — do not modify or delete
    - [x] 5.4: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — no regressions

- [x] Task 6: Create idempotent event processing guide (AC: #5)
    - [x] 6.1: Create `docs/idempotent-event-processing.md`
    - [x] 6.2: Content must include: at-least-once delivery explanation (DAPR pub/sub), deduplication by event ID (MessageId/ULID), idempotent handler pattern with code sample referencing `TenantEventProcessor`
    - [x] 6.3: Include guidance on production deduplication (bounded LRU cache, external deduplication store) vs. MVP in-memory dictionary

- [x] Task 7: Build verification (all ACs)
    - [x] 7.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 7.2: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

## Dev Notes

### Scope: Sample Service + Documentation

This story creates a reference implementation sample and idempotent processing documentation. Stories 4.1 and 4.2 built the Client infrastructure (DI, event handlers, projections, subscription endpoints). This story **uses** that infrastructure — it does NOT modify Client, Contracts, Server, or CommandApi source code.

**This story does NOT:**

- Modify any `src/` project source files — only `samples/` and `docs/`
- Create server-side projections (Epic 5)
- Add the sample to AppHost orchestration (that's Epic 7 — Aspire hosting)
- Create the "aha moment" demo (FR63) — that's Story 8.3

### Current State: Scaffold Only

The sample project exists with:

- `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` — Web SDK, references Client + Contracts
- `samples/Hexalith.Tenants.Sample/Program.cs` — empty 3-line scaffold: `var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.Run();`
- `samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj` — references Sample + Testing, has xUnit/Shouldly/NSubstitute
- `samples/Hexalith.Tenants.Sample.Tests/ScaffoldingSmokeTests.cs` — single placeholder test

### Previous Story Intelligence

**Story 4.1 (done) — Client DI Registration:**

- Created `AddHexalithTenants()` with two overloads (parameterless + `Action<HexalithTenantsOptions>`)
- Registers DaprClient, HexalithTenantsOptions, is idempotent
- Changed `Dapr.Client` → `Dapr.AspNetCore` in Client.csproj (needed for `AddDaprClient()`)
- Tests verify descriptor existence, NOT resolution (DaprClient needs gRPC sidecar)
- **Debug fix:** Order-dependent DI issue resolved — core registrations always ensured first

**Story 4.2 (review) — Event Subscription & Local Projection:**

- Extended `AddHexalithTenants()` with event handler + projection + processor registrations
- Created `ITenantEventHandler<TEvent>`, `TenantEventContext`, `TenantProjectionEventHandler`
- Created `ITenantProjectionStore`, `InMemoryTenantProjectionStore`, `TenantLocalState`
- Created `TenantEventProcessor` (dispatch + deduplication by MessageId)
- Created `TenantEventSubscriptionEndpoints.MapTenantEventSubscription()` — uses `WithTopic()` on minimal API
- `FrameworkReference Include="Microsoft.AspNetCore.App"` added to Client.csproj
- Idempotency: `ConcurrentDictionary<string, byte>` — unbounded for MVP, documented for production

### Architecture Compliance

**Sample project location:** `samples/Hexalith.Tenants.Sample/` (FR62) — already scaffolded
**Sample tests location:** `samples/Hexalith.Tenants.Sample.Tests/` — already scaffolded
**Documentation location:** `docs/` — does NOT exist yet, must be created

**Type Location Rules (MUST follow):**

| Type                           | Project      | Folder     | File                                       |
| ------------------------------ | ------------ | ---------- | ------------------------------------------ |
| Sample Program.cs              | Sample       | root       | Program.cs (MODIFY)                        |
| AccessCheckEndpoints           | Sample       | Endpoints/ | AccessCheckEndpoints.cs (CREATE)           |
| SampleLoggingEventHandler      | Sample       | Handlers/  | SampleLoggingEventHandler.cs (CREATE)      |
| AccessCheckEndpointsTests      | Sample.Tests | Endpoints/ | AccessCheckEndpointsTests.cs (CREATE)      |
| SampleLoggingEventHandlerTests | Sample.Tests | Handlers/  | SampleLoggingEventHandlerTests.cs (CREATE) |
| Idempotent guide               | docs         | root       | idempotent-event-processing.md (CREATE)    |

**DO NOT:**

- Modify any `src/` project source files — this story is samples + docs only
- Add the sample to AppHost orchestration (Epic 7)
- Create integration tests requiring DAPR sidecar — all tests must be Tier 1 (no infrastructure)
- Add unnecessary NuGet packages to the sample beyond what's needed
- Create a new project — only modify existing sample scaffold

### Sample Program.cs — Reference Implementation

The sample's `Program.cs` must demonstrate the complete consuming service setup in minimal code:

```csharp
// samples/Hexalith.Tenants.Sample/Program.cs
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Sample.Handlers;

var builder = WebApplication.CreateBuilder(args);

// 1. Register all tenant client services (DaprClient, options, event handlers, projections)
builder.Services.AddHexalithTenants();

// 2. Register sample-specific logging handler (demonstrates extensibility)
builder.Services.AddSingleton<SampleLoggingEventHandler>();
// Register for specific event types the sample wants to log
// (pattern matches Story 4.2 handler registration)

var app = builder.Build();

// 3. Enable CloudEvents middleware (required for DAPR pub/sub)
app.UseCloudEvents();

// 4. Map DAPR subscription handler (discovers subscriptions)
app.MapSubscribeHandler();

// 5. Map tenant event subscription endpoint
app.MapTenantEventSubscription();

// 6. Map sample access-check endpoint
app.MapAccessCheckEndpoints();

app.Run();
```

**CRITICAL — Total DI+middleware lines must be under 20 (FR45).** The above pattern achieves this with ~12 lines of meaningful code.

**IMPORTANT — `UseCloudEvents()` and `MapSubscribeHandler()`:** These come from `Dapr.AspNetCore`. The sample's csproj already references Client which references `Dapr.AspNetCore`, but the sample itself may need a direct `Dapr.AspNetCore` package reference for the middleware extension methods. Check if they're available transitively. If not, add the package reference.

### Access Check Endpoint Pattern

```csharp
// samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.Tenants.Sample.Endpoints;

public static class AccessCheckEndpoints
{
    public static IEndpointRouteBuilder MapAccessCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/access/{tenantId}/{userId}", async (
            string tenantId,
            string userId,
            ITenantProjectionStore store,
            CancellationToken cancellationToken) =>
        {
            TenantLocalState? state = await store.GetAsync(tenantId, cancellationToken);
            if (state is null)
            {
                return Results.NotFound(new { TenantId = tenantId, Message = "Tenant not found in local projection" });
            }

            if (state.Status == TenantStatus.Disabled)
            {
                return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "denied", Reason = "Tenant is disabled" });
            }

            if (!state.Members.TryGetValue(userId, out TenantRole role))
            {
                return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "denied", Reason = "User is not a member" });
            }

            return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "granted", Role = role.ToString() });
        });

        return endpoints;
    }
}
```

This demonstrates **access enforcement from local projection** — the core "aha moment" that consuming services query local state, not the central tenant service.

### Sample Logging Event Handler

```csharp
// samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Sample.Handlers;

/// <summary>
/// Sample handler that logs tenant events. Demonstrates how consuming services
/// can register additional handlers alongside the built-in projection handler.
/// </summary>
public class SampleLoggingEventHandler :
    ITenantEventHandler<UserAddedToTenant>,
    ITenantEventHandler<UserRemovedFromTenant>,
    ITenantEventHandler<TenantDisabled>
{
    private readonly ILogger<SampleLoggingEventHandler> _logger;

    public SampleLoggingEventHandler(ILogger<SampleLoggingEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Sample] User {UserId} added to tenant {TenantId} with role {Role}",
            @event.UserId, context.TenantId, @event.Role);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserRemovedFromTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[Sample] User {UserId} REMOVED from tenant {TenantId} — revoking access",
            @event.UserId, context.TenantId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TenantDisabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[Sample] Tenant {TenantId} DISABLED — blocking all operations",
            context.TenantId);
        return Task.CompletedTask;
    }
}
```

**CRITICAL — Handler registration pattern:** The sample must register `SampleLoggingEventHandler` as `ITenantEventHandler<T>` for each event type in DI, following the same pattern used in `TenantServiceCollectionExtensions.EnsureEventHandlerRegistrations()`:

```csharp
// In Program.cs or a registration helper:
builder.Services.AddSingleton<SampleLoggingEventHandler>();
builder.Services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<TenantDisabled>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
```

This registers the logging handler as an **additional** `ITenantEventHandler<T>` alongside the built-in `TenantProjectionEventHandler`. The `TenantEventProcessor` dispatches to ALL registered handlers via `IServiceProvider.GetServices<ITenantEventHandler<T>>()`.

### Sample.csproj Dependencies

Current state already has:

- `ProjectReference` to `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts`
- Web SDK (`Microsoft.NET.Sdk.Web`)

May need:

- `Dapr.AspNetCore` — for `UseCloudEvents()`, `MapSubscribeHandler()`. Check if available transitively through Client. If `UseCloudEvents()` / `MapSubscribeHandler()` do not compile without a direct reference, add `<PackageReference Include="Dapr.AspNetCore" />`.

### Idempotent Event Processing Guide Content (FR42)

Create `docs/idempotent-event-processing.md` with:

1. **Why idempotency matters** — DAPR pub/sub guarantees at-least-once delivery, NOT exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times.

2. **How Hexalith.Tenants.Client handles it** — `TenantEventProcessor` tracks processed `MessageId` values (ULID) in a `ConcurrentDictionary`. When a duplicate event arrives, it's silently skipped. The `MessageId` is a unique event identifier set by EventStore at persistence time.

3. **Code example** — Show the deduplication flow from `TenantEventProcessor.ProcessAsync()`:

    ```csharp
    // 1. Check if already processed
    if (!_processedMessageIds.TryAdd(envelope.MessageId, 0))
    {
        // Duplicate — skip
        return false;
    }
    // 2. Proceed with normal processing...
    ```

4. **Making handlers idempotent** — Even with message-level deduplication, handlers should be designed for idempotent application:
    - Setting a dictionary value (`state.Members[userId] = role`) is inherently idempotent
    - Removing a key (`state.Members.Remove(userId)`) is inherently idempotent
    - Avoid side effects that aren't idempotent (e.g., sending emails) without external deduplication

5. **Production considerations** — The in-memory `ConcurrentDictionary` grows unboundedly and resets on restart. For production:
    - Use a bounded LRU cache (e.g., `MemoryCache` with time-based expiration)
    - Use an external deduplication store (Redis, database) keyed by `MessageId`
    - Combine with handler-level idempotency for defense-in-depth

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

Test the sample's **logic**, not DAPR integration (that's Tier 3 in Epic 7).

**AccessCheckEndpointsTests:**

- Test access granted for member with role
- Test access denied for non-member
- Test access denied for disabled tenant
- Test 404 for unknown tenant
- Use `InMemoryTenantProjectionStore` directly (already available via Testing project reference)

**SampleLoggingEventHandlerTests:**

- Test each handler method can be called without throwing
- Verify logging output using `ILogger<T>` mock or test logger
- Focus on: handler receives correct event data in context

**Test pattern — DO NOT use WebApplicationFactory for Tier 1.** Tests should instantiate handler/store directly and test the logic. WebApplicationFactory tests require DAPR sidecar for middleware and are Tier 3.

```csharp
// Example: AccessCheckEndpointsTests pattern
// Use InMemoryTenantProjectionStore directly
var store = new InMemoryTenantProjectionStore();
await store.SaveAsync(new TenantLocalState
{
    TenantId = "acme",
    Name = "Acme",
    Status = TenantStatus.Active,
    Members = { ["user1"] = TenantRole.TenantOwner }
});

// Test the access logic directly (extract helper from endpoint, or test through
// a minimal wrapper that takes ITenantProjectionStore)
```

### Library & Framework Requirements

**Sample — Potentially needs `Dapr.AspNetCore` direct reference:**

- `UseCloudEvents()` and `MapSubscribeHandler()` extension methods are in `Dapr.AspNetCore`
- Client.csproj has `<PackageReference Include="Dapr.AspNetCore" />` — check if this flows transitively to the Sample project via ProjectReference. If these methods don't compile, add: `<PackageReference Include="Dapr.AspNetCore" />` to `Hexalith.Tenants.Sample.csproj`
- Version is centrally managed in `Directory.Packages.props` — do NOT specify version in csproj

**Sample.Tests — No new packages needed:**

- xUnit, Shouldly, NSubstitute already in csproj
- `Microsoft.NET.Test.Sdk` already present
- References Sample + Testing projects

### File Structure Requirements

```text
samples/Hexalith.Tenants.Sample/
├── Hexalith.Tenants.Sample.csproj          (EXISTS — may need Dapr.AspNetCore)
├── Program.cs                              (MODIFY — replace scaffold)
├── Endpoints/
│   └── AccessCheckEndpoints.cs             (CREATE)
└── Handlers/
    └── SampleLoggingEventHandler.cs        (CREATE)

samples/Hexalith.Tenants.Sample.Tests/
├── Hexalith.Tenants.Sample.Tests.csproj    (EXISTS — no changes)
├── ScaffoldingSmokeTests.cs                (EXISTS — do not modify)
├── Endpoints/
│   └── AccessCheckEndpointsTests.cs        (CREATE)
└── Handlers/
    └── SampleLoggingEventHandlerTests.cs   (CREATE)

docs/
└── idempotent-event-processing.md          (CREATE — FR42)
```

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all public method reference parameters
- XML doc comments: minimal — `/// <summary>` on public classes in the sample (it's a sample, not a NuGet package API)
- The sample should be **concise and readable** — it's documentation-as-code

### Cross-Story Dependencies

**This story depends on:**

- Story 4.1 (done): `AddHexalithTenants()` DI registration
- Story 4.2 (review): Event handlers, projections, `MapTenantEventSubscription()`, `TenantEventProcessor`
- Story 1.1 (done): Solution structure, Sample.csproj scaffold

**Stories that depend on this:**

- Story 7.1: Aspire hosting — may add sample to AppHost orchestration
- Story 8.1: Quickstart guide — references sample as the "getting started" path
- Story 8.3: "Aha moment" demo — uses the sample service running

### Critical Anti-Patterns (DO NOT)

- **DO NOT** modify any `src/` project files — this story only touches `samples/` and `docs/`
- **DO NOT** create integration tests requiring DAPR sidecar — Tier 1 only
- **DO NOT** duplicate Client infrastructure — use `AddHexalithTenants()` and `MapTenantEventSubscription()` from Client
- **DO NOT** create a custom `ITenantProjectionStore` implementation — use the built-in `InMemoryTenantProjectionStore` registered by `AddHexalithTenants()`
- **DO NOT** implement complex access control logic — the sample demonstrates **projection-based reads**, not a full authorization system
- **DO NOT** add logging to the Client library code — logging belongs in the sample's handlers and Program.cs
- **DO NOT** create separate DI extension methods in the sample — keep all DI in Program.cs for clarity
- **DO NOT** add the sample to AppHost `Program.cs` — that's Epic 7 scope

### Git Intelligence

Recent commits show Stories 3.2, 3.3, 4.1 complete. Story 4.2 is in review status. The codebase pattern is: contracts in Contracts project, domain logic in Server, client infrastructure in Client, composition in Program.cs. Tests follow Given/When/Then pattern with Shouldly assertions.

### Project Structure Notes

- Sample project uses `Microsoft.NET.Sdk.Web` (already configured) — supports minimal API, middleware
- Sample.Tests references both Sample and Testing projects — can use `InMemoryTenantProjectionStore` from Client and test helpers from Testing
- `Directory.Packages.props` manages all NuGet versions centrally
- The `docs/` directory does not exist yet and must be created as part of Task 6

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4] — Epic objectives: consuming service support
- [Source: _bmad-output/planning-artifacts/prd.md#FR42] — Idempotent event processing guidance documentation
- [Source: _bmad-output/planning-artifacts/prd.md#FR62] — Sample consuming service demonstrating event subscription
- [Source: _bmad-output/planning-artifacts/architecture.md#Consuming Service Flow] — `DAPR pub/sub → Service Subscription → Local Projection → Service-specific behavior`
- [Source: _bmad-output/planning-artifacts/architecture.md#Directory Structure] — `samples/Hexalith.Tenants.Sample/` for FR62
- [Source: _bmad-output/planning-artifacts/architecture.md#Component Boundaries] — Client → References Contracts only
- [Source: _bmad-output/implementation-artifacts/4-1-client-di-registration.md] — DI foundation patterns and learnings
- [Source: _bmad-output/implementation-artifacts/4-2-event-subscription-and-local-projection-pattern.md] — Event handler and subscription infrastructure
- [Source: src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs] — `AddHexalithTenants()` implementation
- [Source: src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs] — `MapTenantEventSubscription()` implementation
- [Source: src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs] — Deduplication logic for documentation
- [Source: samples/Hexalith.Tenants.Sample/Program.cs] — Current scaffold to replace
- [Source: samples/Hexalith.Tenants.Sample.Tests/ScaffoldingSmokeTests.cs] — Existing test to preserve

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Initial build failed with CA1062 (null validation on handler parameters) and CA2007 (ConfigureAwait) — fixed by adding `ArgumentNullException.ThrowIfNull()` guards and `.ConfigureAwait(false)`
- AccessCheckEndpoints tests initially used `ShouldBeOfType<Ok<object>>()` but `Results.Ok(anonymousType)` returns `Ok<AnonymousType>` — fixed by using `IStatusCodeHttpResult`/`IValueHttpResult` interfaces
- `Dapr.AspNetCore` confirmed to flow transitively from Client project reference — no direct package reference needed in Sample.csproj
- Task 4.2 verified: `UseCloudEvents()` and `MapSubscribeHandler()` compile without direct Dapr.AspNetCore reference
- Follow-up code review fixes added whitespace input validation for `/access/{tenantId}/{userId}`, processor-driven projection tests, and an explicit handler code sample in the idempotency guide

### Completion Notes List

- Implemented complete sample consuming service (Program.cs ~12 lines of meaningful code, under FR45 20-line limit)
- Created `AccessCheckEndpoints` with `CheckAccessAsync` static method — demonstrates projection-based access enforcement with proper testability
- Created `SampleLoggingEventHandler` implementing `ITenantEventHandler<T>` for 3 event types — demonstrates extensibility with multiple handlers per event
- Handler DI registration in Program.cs follows exact pattern from `TenantServiceCollectionExtensions.RegisterEventHandler<T,THandler>()`
- 15 tests now cover endpoint logic, processor-driven projection updates, and handler logging. ScaffoldingSmokeTests preserved
- Idempotent event processing guide covers: at-least-once delivery, TenantEventProcessor deduplication flow, handler idempotency patterns, production considerations (LRU cache, Redis, defense-in-depth)
- No `src/` files modified — only `samples/` and `docs/` as required
- Pre-existing integration test failures (DaprEndToEndTests) are unrelated — require DAPR sidecar infrastructure

### Change Log

- 2026-03-18: Story 4.3 implementation complete — sample consuming service, access-check endpoint, logging handler, 12 unit tests, idempotent processing guide
- 2026-03-18: Follow-up review fixes — whitespace validation, processor-driven sample tests, documentation code sample, sprint status cleanup
- 2026-03-18: Story status advanced to done after review and focused validation passed

### File List

- samples/Hexalith.Tenants.Sample/Program.cs (MODIFIED — replaced scaffold with full consuming service setup)
- samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs (CREATED — projection-based access enforcement endpoint)
- samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs (CREATED — extensibility demo logging handler)
- samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs (CREATED — 5 unit tests)
- samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs (CREATED — 7 unit tests)
- docs/idempotent-event-processing.md (CREATED — FR42 idempotent processing guidance)
