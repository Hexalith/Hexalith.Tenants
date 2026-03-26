# Story 4.1: Client DI Registration

Status: done

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

- [x] Task 1: Create `HexalithTenantsOptions.cs` (AC: #1) — BUILD FIRST: extension method depends on this
    - [x] 1.1: Create `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs` — options class for consuming service configuration
    - [x] 1.2: Properties: `PubSubName` (string, default `"pubsub"`), `TopicName` (string, default `"system.tenants.events"`), `Hexalith.TenantsAppId` (string, default `"commandapi"`)
    - [x] 1.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create `TenantServiceCollectionExtensions.cs` (AC: #1, #2, #3) — depends on Task 1
    - [x] 2.1: Create `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` with `AddHexalithTenants(this IServiceCollection)` extension method
    - [x] 2.2: Register DaprClient via `AddDaprClient()` with idempotency guard (skip if already registered)
    - [x] 2.3: Bind `HexalithTenantsOptions` from configuration section `"Tenants"` using `IConfiguration` opportunistic resolution
    - [x] 2.4: Add `AddHexalithTenants(this IServiceCollection, Action<HexalithTenantsOptions>)` overload
    - [x] 2.5: Return `IServiceCollection` for fluent chaining (both overloads)
    - [x] 2.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create unit tests (AC: #4)
    - [x] 3.0: `Microsoft.Extensions.Configuration.Memory` NOT needed — `AddInMemoryCollection()` available transitively from `Microsoft.Extensions.Configuration` in .NET 10
    - [x] 3.1: Create `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs`
    - [x] 3.2: Test: `AddHexalithTenants_RegistersDaprClient` — verifies DaprClient descriptor exists (DO NOT resolve — see Dev Notes)
    - [x] 3.3: Test: `AddHexalithTenants_BindsTenantsOptions` — verifies `HexalithTenantsOptions` is bound from config
    - [x] 3.4: Test: `AddHexalithTenants_IsIdempotent` — calling twice does not duplicate registrations
    - [x] 3.5: Test: `AddHexalithTenants_ReturnsSameServiceCollection` — fluent chaining works
    - [x] 3.6: Test: `AddHexalithTenants_DefaultOptionsValues` — verify default PubSubName, TopicName, Hexalith.TenantsAppId
    - [x] 3.7: Test: `AddHexalithTenants_ThrowsOnNullServices` — null guard for parameterless overload
    - [x] 3.8: Test: `AddHexalithTenants_WithAction_ThrowsOnNullServices` — null guard for action overload
    - [x] 3.9: Test: `AddHexalithTenants_WithAction_ThrowsOnNullAction` — null guard for action parameter
    - [x] 3.10: Test: `AddHexalithTenants_ConfigExistsButNoTenantsSection` — options resolve with defaults when config section is absent
    - [x] 3.11: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [x] Task 4: Build verification (all ACs)
    - [x] 4.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 4.2: `dotnet test Hexalith.Tenants.slnx` — all Tier 1+2 pass (172/172), no regressions. 2 pre-existing Tier 3 integration test failures (DaprEndToEndTests — requires running DAPR sidecar)

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

Architecture specifies: "Client → References Contracts only (thin DI layer)". This describes the _project reference_ layer — Client has no project reference to Server, Hexalith.Tenants, or any other Hexalith project except Contracts. NuGet infrastructure packages (`Dapr.Client`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Hosting.Abstractions`) are already in the csproj from Story 1.1 and are not "references" in the architecture sense. The Client package is a NuGet package consumed by external services. It must be minimal — only DI extension methods and configuration options.

Architecture directory structure prescribes exactly:

```
src/Hexalith.Tenants.Client/
├── Hexalith.Tenants.Client.csproj
└── Registration/
    └── TenantServiceCollectionExtensions.cs  # FR44, FR45
```

This story adds the `Registration/` folder and `TenantServiceCollectionExtensions.cs`, plus a `Configuration/` folder for the options class.

### Extension Method Pattern — Follow EventStore Convention

Follow the _logical pattern_ from `EventStoreServiceCollectionExtensions.cs` (`Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/`):

1. Static class with `this IServiceCollection` extension methods
2. `ArgumentNullException.ThrowIfNull(services)` guard
3. Idempotency guard (check if already registered before adding)
4. Opportunistic `IConfiguration` binding (resolve from service collection if available)
5. Return `IServiceCollection` for fluent chaining

**IMPORTANT — Brace style:** The EventStore repo uses K&R braces (opening brace on same line). This project uses **Allman braces** (new line before opening brace) per `.editorconfig`. Follow the EventStore's logical pattern but use Allman braces throughout.

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

    public string Hexalith.TenantsAppId { get; set; } = "commandapi";
}
```

**Design rationale:**

- `PubSubName`: DAPR pub/sub component name. Default `"pubsub"` matches the DAPR component defined in the Aspire topology (`AddDaprPubSub("pubsub")`).
- `TopicName`: The event topic. Architecture specifies `"system.tenants.events"` as the single topic for all tenant events.
- `Hexalith.TenantsAppId`: The DAPR app ID of the tenant Hexalith.Tenants service. Default `"commandapi"` matches the Aspire AppHost configuration.

### DaprClient Registration

The `AddDaprClient()` method is from the `Dapr.Client` NuGet package (version 1.17.3, managed by `Directory.Packages.props`). It registers:

- `DaprClient` as a singleton
- `DaprClientBuilder` for configuration

The idempotency check prevents double registration if the consuming service already called `AddDaprClient()` directly.

**IMPORTANT — Tier 1 Test Constraint:** `DaprClient` is abstract and its concrete implementation (`DaprClientGrpc`) requires gRPC channel setup and a running DAPR sidecar. DO NOT call `BuildServiceProvider().GetService<DaprClient>()` in unit tests — it will throw. Instead, verify the `ServiceDescriptor` exists in the collection: `services.ShouldContain(s => s.ServiceType == typeof(DaprClient))`.

### Design Decisions

- **Idempotency sentinel:** `IConfigureOptions<HexalithTenantsOptions>` (not a private marker). Acceptable for a thin 2-file package; one-line fix if .NET changes the registration shape.
- **First registration wins:** Calling `AddHexalithTenants()` twice silently skips the second call (standard .NET DI convention). Consumers needing custom config should call the `Action<T>` overload first.
- **`BuildServiceProvider()` in `TryGetConfiguration`:** Copied from EventStore. `ASP0000` analyzer only fires in web projects; Client is a class library (`Microsoft.NET.Sdk`), so no warning.

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

| Type                              | Project      | Folder         | File                                               |
| --------------------------------- | ------------ | -------------- | -------------------------------------------------- |
| TenantServiceCollectionExtensions | Client       | Registration/  | TenantServiceCollectionExtensions.cs (CREATE)      |
| HexalithTenantsOptions            | Client       | Configuration/ | HexalithTenantsOptions.cs (CREATE)                 |
| DI tests                          | Client.Tests | Registration/  | TenantServiceCollectionExtensionsTests.cs (CREATE) |

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
- Modify Hexalith.Tenants' Program.cs — this story is about consuming service DI, not the command API itself
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

**Version fallback:** If `dotnet restore` fails for version `10.0.3`, check NuGet for the latest available `10.x` version of `Microsoft.Extensions.Configuration.Memory` and use that instead. The version should be compatible with `Microsoft.Extensions.Configuration.Binder` 10.0.3 already in `Directory.Packages.props`.

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

**Required test file imports:**

```csharp
using Dapr.Client;

using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Registration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;
```

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

| #   | Test                                                      | Setup                                                                 | Expected                                                                             | AC     |
| --- | --------------------------------------------------------- | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ------ |
| D1  | AddHexalithTenants registers DaprClient                   | ServiceCollection + IConfiguration                                    | ServiceDescriptor for `DaprClient` exists (DO NOT resolve — gRPC needs DAPR sidecar) | #1, #3 |
| D2  | AddHexalithTenants binds options from config              | ServiceCollection + IConfiguration with `Tenants:PubSubName=mypubsub` | `IOptions<HexalithTenantsOptions>.Value.PubSubName == "mypubsub"`                    | #1, #3 |
| D3  | AddHexalithTenants is idempotent                          | Call twice on same ServiceCollection                                  | No duplicate registrations for `HexalithTenantsOptions`                              | #3     |
| D4  | AddHexalithTenants returns same collection                | Any ServiceCollection                                                 | Return value is same reference as input                                              | #2     |
| D5  | Default options have correct values                       | No config section                                                     | PubSubName="pubsub", TopicName="system.tenants.events", Hexalith.TenantsAppId="commandapi" | #3     |
| D6  | AddHexalithTenants with configure action                  | Call overload with `o => o.PubSubName = "custom"`                     | `IOptions<HexalithTenantsOptions>.Value.PubSubName == "custom"`                      | #1, #2 |
| D7  | AddHexalithTenants skips DaprClient if already registered | Pre-register DaprClient, then call AddHexalithTenants                 | No duplicate DaprClient descriptors (check descriptor count)                         | #3     |
| D8  | AddHexalithTenants works without IConfiguration           | Empty ServiceCollection (no IConfiguration)                           | Options registered with defaults, no exception                                       | #1     |
| D9  | AddHexalithTenants throws on null services                | `null` ServiceCollection                                              | `ArgumentNullException`                                                              | #1     |
| D10 | AddHexalithTenants with action throws on null services    | `null` ServiceCollection                                              | `ArgumentNullException`                                                              | #1     |
| D11 | AddHexalithTenants with action throws on null action      | Valid ServiceCollection, `null` action                                | `ArgumentNullException`                                                              | #1     |
| D12 | AddHexalithTenants with config but no Tenants section     | IConfiguration with unrelated keys only                               | Options resolve with all defaults (PubSubName="pubsub", etc.)                        | #3     |

**Note:** `tests/Hexalith.Tenants.Client.Tests/ScaffoldingSmokeTests.cs` already exists with a placeholder test. New test files should coexist with it. Do not modify or delete it.

**Key assertion patterns (non-obvious tests only — straightforward tests like D2-D6 follow standard Shouldly conventions):**

```csharp
// D1: DaprClient registered — DESCRIPTOR check only, DO NOT call BuildServiceProvider().GetService<DaprClient>()
// DaprClient resolution requires gRPC + DAPR sidecar — not available in Tier 1 tests.
var services = CreateServiceCollectionWithConfig();
services.AddHexalithTenants();
services.ShouldContain(s => s.ServiceType == typeof(DaprClient));

// D3: Idempotency — check IConfigureOptions<T> count (what Configure<T>() actually registers)
var services = CreateServiceCollectionWithConfig();
services.AddHexalithTenants();
services.AddHexalithTenants();
services.Count(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)).ShouldBe(1);

// D7: DaprClient idempotency — descriptor count check
var services = CreateServiceCollectionWithConfig();
services.AddDaprClient();
int daprCountBefore = services.Count(s => s.ServiceType == typeof(DaprClient));
services.AddHexalithTenants();
int daprCountAfter = services.Count(s => s.ServiceType == typeof(DaprClient));
daprCountAfter.ShouldBe(daprCountBefore);

// D9: Null guard — must use static call syntax (extension method on null is invalid)
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

**Story 2.4 (done) — Hexalith.Tenants Bootstrap & Event Publishing:**

- Established Hexalith.Tenants `Program.cs` DI composition root pattern
- Shows how `AddDaprClient()`, `AddEventStore()`, `AddEventStoreServer()` are composed
- The Client DI method should be composable with these — consuming services may also use EventStore

**Story 1.1 (done) — Solution Structure:**

- Established `Directory.Build.props` (TreatWarningsAsErrors, net10.0, MinVer)
- Established `Directory.Packages.props` (centralized NuGet management)
- Client.csproj was created as part of this story with all dependencies pre-configured

### Git Intelligence

Recent commits show:

- `fd1b5d9 feat: Finalize Hexalith.Tenants Bootstrap & Event Publishing` — Story 2.4 complete
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
- [Source: src/Hexalith.Tenants/Program.cs] — Current DI composition root showing how services are composed
- [Source: _bmad-output/implementation-artifacts/3-3-tenant-configuration-management.md] — Previous story patterns and learnings

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- `AddDaprClient()` extension method is in `Dapr.AspNetCore` package, not `Dapr.Client`. Story spec incorrectly stated no new packages needed. Resolved by replacing `Dapr.Client` with `Dapr.AspNetCore` in Client.csproj (user approved option 1).
- `Microsoft.Extensions.Configuration.Memory` package does not exist in .NET 10 — `AddInMemoryCollection()` is available transitively from `Microsoft.Extensions.Configuration`. No package addition needed for tests.
- Code review surfaced an order-dependent DI issue: an early `IConfigureOptions<HexalithTenantsOptions>` sentinel could suppress required `DaprClient` registration and block later configuration binding. Resolved by always ensuring core registrations and only deduplicating the options configuration step.

### Completion Notes List

- Created `HexalithTenantsOptions` with defaults: PubSubName="pubsub", TopicName="system.tenants.events", Hexalith.TenantsAppId="commandapi"
- Created `TenantServiceCollectionExtensions` with two overloads: parameterless (config binding) and `Action<T>` (explicit config)
- Refined registration flow after review: core registrations (`DaprClient` + options infrastructure) are always ensured, while `IConfigureOptions<HexalithTenantsOptions>` is used only to deduplicate the options configuration step
- DaprClient registration via `AddDaprClient()` remains type-safe and is no longer skipped when options were configured earlier
- Opportunistic `IConfiguration` binding via `TryGetConfiguration` helper (follows EventStore pattern)
- Added regression coverage for service lifetimes and order-dependent registration/configuration scenarios
- 16 unit tests covering the DI contract: DaprClient registration, lifetimes, options binding, idempotency, fluent chaining, defaults, null guards, missing config section, action overload, preconfigured options, and late configuration binding
- Changed `Dapr.Client` → `Dapr.AspNetCore` in Client.csproj (required for `AddDaprClient()` extension)
- Validation rerun: `dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --no-restore` → 16/16 tests passed

### File List

- `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs` — NEW
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` — NEW
- `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` — MODIFIED (Dapr.Client → Dapr.AspNetCore)
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` — NEW

### Change Log

- 2026-03-17: Story 4.1 implemented — Client DI registration with HexalithTenantsOptions, TenantServiceCollectionExtensions, and 12 unit tests
- 2026-03-17: Review follow-up — fixed order-dependent DI registration behavior, added lifetime/regression tests, and revalidated `Hexalith.Tenants.Client.Tests` (16/16 passing)
