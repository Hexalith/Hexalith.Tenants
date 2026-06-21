using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Server.Aggregates;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Integration test fixture that starts the Tenants CommandApi with a local daprd sidecar,
/// reusing the DAPR infrastructure (Redis, placement, scheduler) from dapr init.
/// Tests the full command pipeline: Actor → Domain Service Invocation → /process → Aggregate → Events.
/// </summary>
/// <remarks>
/// All DAPR sidecar plumbing — port allocation, sidecar lifecycle, health probing, prerequisite
/// checks, support-safe diagnostics, the actor/<c>/process</c>/<c>/healthz</c> endpoints — lives in the
/// reusable platform base <see cref="DaprDomainServiceTestFixtureBase"/>. This fixture supplies only the
/// Tenants domain host registration and the publisher/store fakes the tests assert against.
/// </remarks>
public sealed class TenantsDaprTestFixture : DaprDomainServiceTestFixtureBase {
    /// <inheritdoc/>
    protected override string AppId => "commandapi";

    /// <inheritdoc/>
    protected override string DeadLetterTopic => "deadletter.tenants.events";

    /// <summary>Gets the fake event publisher for capturing published events.</summary>
    public TestEventPublisher EventPublisher { get; } = new();

    /// <summary>Gets the fake dead-letter publisher for test assertions.</summary>
    public FakeDeadLetterPublisher DeadLetterPublisher { get; } = new();

    /// <summary>Gets the in-memory command status store for tracking command lifecycle.</summary>
    public InMemoryCommandStatusStore CommandStatusStore { get; } = new();

    /// <inheritdoc/>
    protected override void ConfigureDomain(WebApplicationBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        // Configure domain service registration: system|tenants|v1 → self (commandapi)
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:AppId"] = AppId;
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:MethodName"] = "process";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:TenantId"] = "system";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Domain"] = "tenants";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Version"] = "v1";
        builder.Configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:AppId"] = AppId;
        builder.Configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:MethodName"] = "process";
        builder.Configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:TenantId"] = "system";
        builder.Configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:Domain"] = TenantIdentity.GlobalAdministratorsDomain;
        builder.Configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:Version"] = "v1";

        // Configure pub/sub name for event publisher
        builder.Configuration["EventStore:Publisher:PubSubName"] = "pubsub";
        builder.Configuration["EventStore:Publisher:TopicOverrides:global-administrators"] = "tenants.events";
        EventPublisher.TopicOverrides[TenantIdentity.GlobalAdministratorsDomain] = "tenants.events";

        // Speed up drain recovery for tests (default is 30s initial / 60s period).
        // Keeps DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers deterministic
        // within its 90s poll budget even if the first reminder tick races ClearFailure().
        builder.Configuration["EventStore:Drain:InitialDrainDelay"] = "00:00:05";
        builder.Configuration["EventStore:Drain:DrainPeriod"] = "00:00:05";

        // Register publisher fakes BEFORE AddEventStoreServer (TryAdd won't override these).
        _ = builder.Services.AddSingleton<IEventPublisher>(EventPublisher);
        _ = builder.Services.AddSingleton<IDeadLetterPublisher>(DeadLetterPublisher);

        // Register DAPR client and EventStore server infrastructure (actors, command routing, REAL domain service invoker)
        builder.Services.AddDaprClient();
        _ = builder.Services.AddEventStoreServer(builder.Configuration);
        _ = builder.Services.RemoveAll<ICommandStatusStore>();
        _ = builder.Services.AddSingleton<ICommandStatusStore>(CommandStatusStore);

        // Register real domain processors (TenantAggregate, GlobalAdministratorsAggregate). The keyed
        // IDomainProcessor registrations back the SDK /process router (DomainServiceRequestRouter).
        _ = builder.Services.AddEventStore(typeof(TenantAggregate).Assembly);

        _ = builder.Services.AddDataProtection()
            .SetApplicationName("Hexalith.Tenants.IntegrationTests");
        builder.Services.AddEventStoreQueryCursorCodec("Hexalith.Tenants.QueryCursor.v1");
    }
}
