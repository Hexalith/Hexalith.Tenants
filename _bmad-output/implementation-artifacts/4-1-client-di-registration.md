# Story 4.1: Client DI Registration

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer building a consuming service,
I want to register tenant client services in my DI container with a single extension method call,
So that my service is wired up for tenant event handling with minimal configuration.

## Acceptance Criteria

1. **Given** a consuming service references the Hexalith.Tenants.Client NuGet package
   **When** the developer calls `services.AddHexalithTenants()` in their DI configuration
   **Then** all required tenant client services (event handlers, abstractions) are registered in the service collection
   _(Note: This AC spans Stories 4.1 + 4.2. Story 4.1 registers DaprClient + configuration options. Story 4.2 adds event handler and subscription registrations. Do NOT implement event handlers in this story.)_

2. **Given** a consuming service references the Hexalith.Tenants.Contracts and Client packages
   **When** the developer registers tenant event handlers
   **Then** the total DI configuration is under 20 lines of code

3. **Given** the Client DI extension method is called
   **When** the service collection is inspected
   **Then** all expected service registrations are present with correct lifetimes

4. **Given** the Client package
   **When** Tier 1 unit tests in Client.Tests are executed
   **Then** DI registration tests verify all services are registered correctly and resolve without errors

## Tasks / Subtasks

- [ ] Task 1: Create `HexalithTenantsOptions.cs` (AC: #1) — BUILD FIRST: extension method depends on this
  - [ ] 1.1: Create `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs` — options class for consuming service configuration
  - [ ] 1.2: Properties: `PubSubName` (string, default `"pubsub"`), `TopicName` (string, default `"system.tenants.events"`), `CommandApiAppId` (string, default `"commandapi"`)
  - [ ] 1.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Create `TenantServiceCollectionExtensions.cs` (AC: #1, #2, #3) — depends on Task 1
  - [ ] 2.1: Create `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` with `AddHexalithTenants(this IServiceCollection)` extension method
  - [ ] 2.2: Register DaprClient via `AddDaprClient()` with idempotency guard (skip if already registered)
  - [ ] 2.3: Bind `HexalithTenantsOptions` from configuration section `"Tenants"` using `IConfiguration` opportunistic resolution
  - [ ] 2.4: Add `AddHexalithTenants(this IServiceCollection, Action<HexalithTenantsOptions>)` overload
  - [ ] 2.5: Return `IServiceCollection` for fluent chaining (both overloads)
  - [ ] 2.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 3: Create unit tests (AC: #4)
  - [ ] 3.0: Add `Microsoft.Extensions.Configuration.Memory` to `Directory.Packages.props` (Version="10.0.3") and to `tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj` — required for `AddInMemoryCollection()` in test helper
  - [ ] 3.1: Create `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs`
  - [ ] 3.2: Test: `AddHexalithTenants_RegistersDaprClient` — verifies DaprClient descriptor exists (DO NOT resolve — see Dev Notes)
  - [ ] 3.3: Test: `AddHexalithTenants_BindsTenantsOptions` — verifies `HexalithTenantsOptions` is bound from config
  - [ ] 3.4: Test: `AddHexalithTenants_IsIdempotent` — calling twice does not duplicate registrations
  - [ ] 3.5: Test: `AddHexalithTenants_ReturnsSameServiceCollection` — fluent chaining works
  - [ ] 3.6: Test: `AddHexalithTenants_DefaultOptionsValues` — verify default PubSubName, TopicName, CommandApiAppId
  - [ ] 3.7: Test: `AddHexalithTenants_ThrowsOnNullServices` — null guard for parameterless overload
  - [ ] 3.8: Test: `AddHexalithTenants_WithAction_ThrowsOnNullServices` — null guard for action overload
  - [ ] 3.9: Test: `AddHexalithTenants_WithAction_ThrowsOnNullAction` — null guard for action parameter
  - [ ] 3.10: Test: `AddHexalithTenants_ConfigExistsButNoTenantsSection` — options resolve with defaults when config section is absent
  - [ ] 3.11: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [ ] Task 4: Build verification (all ACs)
  - [ ] 4.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 4.2: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

## Dev Notes

### Scope: DI Foundation Only

This story creates the Client DI extension method — the single-call entry point for consuming services to register tenant infrastructure. Event subscription logic, local projection building, and event handler abstractions are implemented in Story 4.2 (Event Subscription & Local Projection Pattern). This story is the plumbing; Story 4.2 adds the behavior.

### Current State: Client Project is Empty Shell

The `Hexalith.Tenants.Client` project exists with a `.csproj` file and dependencies already configured, but **has zero source .cs files**. The csproj already references:
- `Hexalith.Tenants.Contracts` (project reference)
- `Dapr.Client` (NuGet — version managed by `Directory.Packages.props`, currently 1.17.3)
- `Microsoft.Extensions.Configuration.Binder` (NuGet)
- `Microsoft.Extensions.Hosting.Abstractions` (NuGet)

All dependencies are ready. No new NuGet packages or project references are needed.

### Architecture: Thin DI Layer

Architecture specifies: "Client → References Contracts only (thin DI layer)". This describes the *project reference* layer — Client has no project reference to Server, CommandApi, or any other Hexalith project except Contracts. NuGet infrastructure packages (`Dapr.Client`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Hosting.Abstractions`) are already in the csproj from Story 1.1 and are not "references" in the architecture sense. The Client package is a NuGet package consumed by external services. It must be minimal — only DI extension methods and configuration options.

Architecture directory structure prescribes exactly:
```
src/Hexalith.Tenants.Client/
├── Hexalith.Tenants.Client.csproj
└── Registration/
    └── TenantServiceCollectionExtensions.cs  # FR44, FR45
```

This story adds the `Registration/` folder and `TenantServiceCollectionExtensions.cs`, plus a `Configuration/` folder for the options class.

### Extension Method Pattern — Follow EventStore Convention

Follow the `EventStoreServiceCollectionExtensions.cs` pattern from the EventStore submodule (`Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/`):

1. Static class with `this IServiceCollection` extension methods
2. `ArgumentNullException.ThrowIfNull(services)` guard
3. Idempotency guard (check if already registered before adding)
4. Opportunistic `IConfiguration` binding (resolve from service collection if available)
5. Return `IServiceCollection` for fluent chaining

```csharp
// src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs
using Hexalith.Tenants.Client.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Registration;

public static class TenantServiceCollectionExtensions
{
    public static IServiceCollection AddHexalithTenants(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Idempotency: skip if already registered.
        // Configure<T>() registers IConfigureOptions<T>, NOT T directly — check the correct sentinel.
        if (services.Any(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)))
        {
            return services;
        }

        // Register DaprClient if not already registered (type-safe check)
        if (!services.Any(s => s.ServiceType == typeof(Dapr.Client.DaprClient)))
        {
            services.AddDaprClient();
        }

        // Opportunistic configuration binding
        IConfiguration? configuration = TryGetConfiguration(services);
        if (configuration is not null)
        {
            services.Configure<HexalithTenantsOptions>(configuration.GetSection("Tenants"));
        }
        else
        {
            services.Configure<HexalithTenantsOptions>(_ => { });
        }

        return services;
    }

    private static IConfiguration? TryGetConfiguration(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ServiceDescriptor? descriptor = services.LastOrDefault(static s => s.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration configurationInstance)
        {
            return configurationInstance;
        }

        if (descriptor is null)
        {
            return null;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        return tempProvider.GetService<IConfiguration>();
    }
}
```

**IMPORTANT:** The `TryGetConfiguration` helper follows the exact pattern from `EventStoreServiceCollectionExtensions.cs` in the EventStore submodule. Do NOT invent a different approach.

**IMPORTANT:** The idempotency check uses `IConfigureOptions<HexalithTenantsOptions>` as the sentinel type — because `services.Configure<T>()` registers `IConfigureOptions<T>`, NOT `T` directly. Do NOT check `typeof(HexalithTenantsOptions)` — that will never match and the idempotency guard will be broken.

**IMPORTANT:** The DaprClient idempotency check uses `typeof(Dapr.Client.DaprClient)` — a type-safe check. Do NOT use string-based type name comparison (`s.ServiceType.Name == "DaprClient"`) as it is fragile and could silently break if Dapr renames the type.

### HexalithTenantsOptions Implementation

```csharp
// src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs
namespace Hexalith.Tenants.Client.Configuration;

public class HexalithTenantsOptions
{
    public string PubSubName { get; set; } = "pubsub";

    public string TopicName { get; set; } = "system.tenants.events";

    public string CommandApiAppId { get; set; } = "commandapi";
}
```

**Design rationale:**
- `PubSubName`: DAPR pub/sub component name. Default `"pubsub"` matches the DAPR component defined in the Aspire topology (`AddDaprPubSub("pubsub")`).
- `TopicName`: The event topic. Architecture specifies `"system.tenants.events"` as the single topic for all tenant events.
- `CommandApiAppId`: The DAPR app ID of the tenant CommandApi service. Default `"commandapi"` matches the Aspire AppHost configuration.

### DaprClient Registration

The `AddDaprClient()` method is from the `Dapr.Client` NuGet package (version 1.17.3, managed by `Directory.Packages.props`). It registers:
- `DaprClient` as a singleton
- `DaprClientBuilder` for configuration

The idempotency check prevents double registration if the consuming service already called `AddDaprClient()` directly.

**IMPORTANT — Tier 1 Test Constraint:** `DaprClient` is abstract and its concrete implementation (`DaprClientGrpc`) requires gRPC channel setup and a running DAPR sidecar. DO NOT call `BuildServiceProvider().GetService<DaprClient>()` in unit tests — it will throw. Instead, verify the `ServiceDescriptor` exists in the collection: `services.ShouldContain(s => s.ServiceType == typeof(DaprClient))`.

### Design Decisions (Party Mode Review)

**Idempotency sentinel: `IConfigureOptions<T>` vs private marker (accepted trade-off):**
`services.Configure<T>()` registers `ConfigureNamedOptions<T>` with `ServiceType == typeof(IConfigureOptions<T>)`. Checking this is an implementation detail of `Microsoft.Extensions.Options`. A private marker record would be more robust (matches the EventStore's `DiscoveryResult` pattern). However, for a thin 2-file package, the simpler approach is acceptable. If this breaks in a future .NET version, the fix is a one-line change. The alternative (private marker) adds a type for no user-facing purpose.

**Idempotency means "first registration wins":**
If a consuming service calls `AddHexalithTenants()` (parameterless, binds from config), then later calls `AddHexalithTenants(o => o.PubSubName = "custom")`, the second call is silently skipped — the options are already registered. This is standard .NET DI convention (same as `AddDbContext`, `AddAuthentication`, and EventStore's `AddEventStore`). Consumers who need custom config should call the `Action<T>` overload first, or call `services.Configure<HexalithTenantsOptions>(action)` separately after.

**`BuildServiceProvider()` in `TryGetConfiguration` — ASP0000 warning:**
This is a known pattern copied verbatim from `EventStoreServiceCollectionExtensions.cs`. The `ASP0000` Roslyn analyzer ("Do not call BuildServiceProvider in ConfigureServices") only fires in ASP.NET Core projects with the web analyzers enabled. The Client project is a class library (`Microsoft.NET.Sdk`), not a web project, so this warning will NOT fire. Accepted technical debt, consistent with EventStore.

### Overload for Action<HexalithTenantsOptions> (REQUIRED — test D6 depends on this)

Provide a configure overload for consuming services that want to customize options without appsettings.json:

```csharp
public static IServiceCollection AddHexalithTenants(
    this IServiceCollection services,
    Action<HexalithTenantsOptions> configureOptions)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configureOptions);

    // Idempotency: skip if already registered (same sentinel as parameterless overload)
    if (services.Any(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)))
    {
        return services;
    }

    // Register DaprClient if not already registered (type-safe check)
    if (!services.Any(s => s.ServiceType == typeof(Dapr.Client.DaprClient)))
    {
        services.AddDaprClient();
    }

    services.Configure(configureOptions);
    return services;
}
```

This overload follows the EventStore pattern (`AddEventStore(Action<EventStoreOptions>)`) and satisfies FR45's "under 20 lines" requirement. **Design note:** This overload intentionally does NOT also bind from `appsettings.json` — the `Action<T>` overload is for explicit configuration, not layered config. If a consuming service wants both file + code config, they call the parameterless overload and then `services.Configure<HexalithTenantsOptions>(action)` separately.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder | File |
|------|---------|--------|------|
| TenantServiceCollectionExtensions | Client | Registration/ | TenantServiceCollectionExtensions.cs (CREATE) |
| HexalithTenantsOptions | Client | Configuration/ | HexalithTenantsOptions.cs (CREATE) |
| DI tests | Client.Tests | Registration/ | TenantServiceCollectionExtensionsTests.cs (CREATE) |

**DO NOT:**
- Create any types outside the Client project — this story is Client-only
- Reference the Server project from Client — Client is a thin DI layer referencing Contracts only
- Add new NuGet packages — all dependencies already in Client.csproj
- Create abstract event handler interfaces — that's Story 4.2 scope
- Create local projection infrastructure — that's Story 4.2 scope
- Add instance state to the extensions class — static methods only
- Add XML doc comments beyond the class-level summary — keep it minimal per project conventions
- Create a separate marker/sentinel class for idempotency — use `IConfigureOptions<HexalithTenantsOptions>` as the sentinel (this is what `Configure<T>()` actually registers)
- Check `typeof(HexalithTenantsOptions)` for idempotency — `Configure<T>()` registers `IConfigureOptions<T>`, not `T` directly
- Use string-based type name comparison for DaprClient check — use `typeof(Dapr.Client.DaprClient)` for type safety
- Modify CommandApi's Program.cs — this story is about consuming service DI, not the command API itself
- Use `IHostApplicationBuilder` as the extension target — use `IServiceCollection` per EventStore convention

### Library & Framework Requirements

**Source (Client) — No new NuGet packages required.**

All dependencies already available in Client.csproj:
- `Dapr.Client` 1.17.3 — `AddDaprClient()` extension method (also transitively provides `Microsoft.Extensions.DependencyInjection` — do NOT add it explicitly)
- `Microsoft.Extensions.Configuration.Binder` — `Configure<T>(IConfigurationSection)` binding
- `Microsoft.Extensions.Hosting.Abstractions` — `IHostApplicationBuilder` (available but not used as extension target)
- `Hexalith.Tenants.Contracts` — contract types available for future event handler registration

**Tests (Client.Tests) — ONE new package required:**

- xUnit 2.9.3 via `tests/Directory.Build.props`
- Shouldly 4.3.0 via `tests/Directory.Build.props`
- **`Microsoft.Extensions.Configuration.Memory`** — MUST ADD. Required for `AddInMemoryCollection()` in test helper `CreateServiceCollectionWithConfig()`. Not transitively available: Client brings `Configuration.Binder` (which brings `Configuration` for `ConfigurationBuilder`), but NOT `Configuration.Memory`.

**Required changes:**
1. Add to `Directory.Packages.props`: `<PackageVersion Include="Microsoft.Extensions.Configuration.Memory" Version="10.0.3" />`
2. Add to `tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj`: `<PackageReference Include="Microsoft.Extensions.Configuration.Memory" />`

### File Structure Requirements

```
src/Hexalith.Tenants.Client/
├── Hexalith.Tenants.Client.csproj          (EXISTS — no changes)
├── Configuration/
│   └── HexalithTenantsOptions.cs           (CREATE)
└── Registration/
    └── TenantServiceCollectionExtensions.cs (CREATE)

tests/Hexalith.Tenants.Client.Tests/
├── Hexalith.Tenants.Client.Tests.csproj    (EXISTS — no changes)
└── Registration/
    └── TenantServiceCollectionExtensionsTests.cs (CREATE)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Test setup pattern:**
```csharp
private static IServiceCollection CreateServiceCollectionWithConfig(
    Dictionary<string, string?>? configValues = null)
{
    var services = new ServiceCollection();
    if (configValues is not null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
    }

    return services;
}
```

**Test matrix:**

| # | Test | Setup | Expected | AC |
|---|------|-------|----------|-----|
| D1 | AddHexalithTenants registers DaprClient | ServiceCollection + IConfiguration | ServiceDescriptor for `DaprClient` exists (DO NOT resolve — gRPC needs DAPR sidecar) | #1, #3 |
| D2 | AddHexalithTenants binds options from config | ServiceCollection + IConfiguration with `Tenants:PubSubName=mypubsub` | `IOptions<HexalithTenantsOptions>.Value.PubSubName == "mypubsub"` | #1, #3 |
| D3 | AddHexalithTenants is idempotent | Call twice on same ServiceCollection | No duplicate registrations for `HexalithTenantsOptions` | #3 |
| D4 | AddHexalithTenants returns same collection | Any ServiceCollection | Return value is same reference as input | #2 |
| D5 | Default options have correct values | No config section | PubSubName="pubsub", TopicName="system.tenants.events", CommandApiAppId="commandapi" | #3 |
| D6 | AddHexalithTenants with configure action | Call overload with `o => o.PubSubName = "custom"` | `IOptions<HexalithTenantsOptions>.Value.PubSubName == "custom"` | #1, #2 |
| D7 | AddHexalithTenants skips DaprClient if already registered | Pre-register DaprClient, then call AddHexalithTenants | No duplicate DaprClient descriptors (check descriptor count) | #3 |
| D8 | AddHexalithTenants works without IConfiguration | Empty ServiceCollection (no IConfiguration) | Options registered with defaults, no exception | #1 |
| D9 | AddHexalithTenants throws on null services | `null` ServiceCollection | `ArgumentNullException` | #1 |
| D10 | AddHexalithTenants with action throws on null services | `null` ServiceCollection | `ArgumentNullException` | #1 |
| D11 | AddHexalithTenants with action throws on null action | Valid ServiceCollection, `null` action | `ArgumentNullException` | #1 |
| D12 | AddHexalithTenants with config but no Tenants section | IConfiguration with unrelated keys only | Options resolve with all defaults (PubSubName="pubsub", etc.) | #3 |

**Note:** `tests/Hexalith.Tenants.Client.Tests/ScaffoldingSmokeTests.cs` already exists with a placeholder test. New test files should coexist with it. Do not modify or delete it.

**Assertion patterns:**
```csharp
// D1: DaprClient registered — DESCRIPTOR check only, DO NOT call BuildServiceProvider().GetService<DaprClient>()
// DaprClient resolution requires gRPC + DAPR sidecar — not available in Tier 1 tests.
var services = CreateServiceCollectionWithConfig();
services.AddHexalithTenants();
services.ShouldContain(s => s.ServiceType == typeof(DaprClient));

// D2: Options bound from config — IOptions<T> resolves without infrastructure
var services = CreateServiceCollectionWithConfig(new Dictionary<string, string?>
{
    ["Tenants:PubSubName"] = "mypubsub"
});
services.AddHexalithTenants();
using var provider = services.BuildServiceProvider();
var options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
options.PubSubName.ShouldBe("mypubsub");

// D3: Idempotency
var services = CreateServiceCollectionWithConfig();
services.AddHexalithTenants();
services.AddHexalithTenants();
services.Count(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)).ShouldBe(1);

// D4: Fluent chaining
var services = new ServiceCollection();
var result = services.AddHexalithTenants();
result.ShouldBeSameAs(services);

// D7: DaprClient idempotency — descriptor count check
var services = CreateServiceCollectionWithConfig();
services.AddDaprClient();
int daprCountBefore = services.Count(s => s.ServiceType == typeof(DaprClient));
services.AddHexalithTenants();
int daprCountAfter = services.Count(s => s.ServiceType == typeof(DaprClient));
daprCountAfter.ShouldBe(daprCountBefore);

// D9: Null guard
Should.Throw<ArgumentNullException>(() =>
    TenantServiceCollectionExtensions.AddHexalithTenants(null!));
```

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all reference type parameters
- No `_ = RuleFor(...)` discard pattern in validators (N/A for this story)
- XML doc comments: ADD `/// <summary>` on the public class and both public extension methods — this is a NuGet package's public API surface and follows the EventStore convention (`EventStoreServiceCollectionExtensions.cs` has XML docs on all public members). Do NOT add XML docs on the private `TryGetConfiguration` helper or on `HexalithTenantsOptions` properties (options classes are self-documenting)

### Previous Story Intelligence

**Story 3.3 (review) — Tenant Configuration Management:**
- Established `ArgumentNullException.ThrowIfNull()` on all reference type parameters
- CA1062 compliance is mandatory — all public method parameters must be null-checked
- `TreatWarningsAsErrors = true` means any CA warnings are build failures

**Story 2.4 (done) — CommandApi Bootstrap & Event Publishing:**
- Established the CommandApi `Program.cs` DI composition root pattern
- Shows how `AddDaprClient()`, `AddEventStore()`, `AddEventStoreServer()` are composed
- The Client DI method should be composable with these — consuming services may also use EventStore

**Story 1.1 (done) — Solution Structure:**
- Established `Directory.Build.props` (TreatWarningsAsErrors, net10.0, MinVer)
- Established `Directory.Packages.props` (centralized NuGet management)
- Client.csproj was created as part of this story with all dependencies pre-configured

### Git Intelligence

Recent commits show:
- `fd1b5d9 feat: Finalize CommandApi Bootstrap & Event Publishing` — Story 2.4 complete
- `9753e09 feat: Implement tenant configuration management` — Story 3.3 implementation
- `79584b5 feat: Add InsufficientPermissionsRejection` — Story 3.2 contracts
- `4216ccd feat: Implement RBAC for tenant management commands` — Story 3.2 implementation

All Epic 2 and 3 stories are done or in review. Epic 4 is the next phase, shifting focus from server-side domain logic to consuming service integration.

### Cross-Story Dependencies

**This story depends on:**
- Epic 1 (done): Solution structure, Client.csproj, build configuration
- Story 2.1 (done): Contract types (commands, events) in Contracts project

**Stories that depend on this:**
- Story 4.2: Event Subscription & Local Projection Pattern — uses `AddHexalithTenants()` as foundation, adds event handler registration
- Story 4.3: Sample Consuming Service — calls `AddHexalithTenants()` in sample DI setup
- Epic 5: Query endpoints — consuming services reference Client package

### Critical Anti-Patterns (DO NOT)

- **DO NOT** reference Server project from Client — Client is a thin layer for consuming services
- **DO NOT** add event handler interfaces/abstractions — that's Story 4.2 scope
- **DO NOT** add projection infrastructure — that's Story 4.2 scope
- **DO NOT** create InMemoryTenantService — that's Story 6.1 scope (Testing package)
- **DO NOT** modify any existing source (.cs) files — this story only creates new source files in Client and Client.Tests (exception: `Directory.Packages.props` and `Client.Tests.csproj` need a package addition — see Library & Framework Requirements)
- **DO NOT** throw exceptions from extension methods (except ArgumentNullException guards)
- **DO NOT** add async methods — DI registration is synchronous
- **DO NOT** use `IHostApplicationBuilder` as extension target — use `IServiceCollection` to match EventStore convention and maximize composability
- **DO NOT** add a `Registration/` namespace suffix to the namespace — the namespace should be `Hexalith.Tenants.Client.Registration` matching the folder structure

### Project Structure Notes

- Alignment with EventStore: mirrors `EventStore.Client/Registration/` folder structure exactly
- Client.Tests.csproj already references Client and Testing projects — no changes needed
- `InternalsVisibleTo` already configured in Client.csproj for Client.Tests
- `Directory.Build.props` in `tests/` sets `IsPackable=false` for test projects
- Solution file (`Hexalith.Tenants.slnx`) already includes Client and Client.Tests — no changes needed

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.1] — Story definition, ACs, BDD scenarios
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4] — Epic objectives: consuming service support
- [Source: _bmad-output/planning-artifacts/prd.md#FR44] — "register tenant client services in DI with a single extension method call"
- [Source: _bmad-output/planning-artifacts/prd.md#FR45] — "register tenant event handlers in under 20 lines of DI configuration"
- [Source: _bmad-output/planning-artifacts/architecture.md#Client] — "References Contracts only (thin DI layer)"
- [Source: _bmad-output/planning-artifacts/architecture.md#Directory Structure] — `Registration/TenantServiceCollectionExtensions.cs` for FR44, FR45
- [Source: _bmad-output/planning-artifacts/architecture.md#Component Boundaries] — Client → References Contracts only
- [Source: _bmad-output/planning-artifacts/architecture.md#Consuming Service Flow] — DAPR pub/sub → Service Subscription → Local Projection
- [Source: src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj] — Empty shell with dependencies pre-configured
- [Source: tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj] — Test project referencing Client + Testing
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs] — DI extension method pattern to follow
- [Source: src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs] — Aspire `AddHexalithTenants()` pattern (different extension point, same naming)
- [Source: src/Hexalith.Tenants.CommandApi/Program.cs] — Current DI composition root showing how services are composed
- [Source: _bmad-output/implementation-artifacts/3-3-tenant-configuration-management.md] — Previous story patterns and learnings

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
