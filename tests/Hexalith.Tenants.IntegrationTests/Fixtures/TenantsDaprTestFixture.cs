using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.EventStore.Testing.Integration;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Server.Aggregates;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Tenants integration-test fixture that hosts the Tenants domain service on the shared EventStore
/// DAPR sidecar harness (<see cref="DaprDomainServiceTestFixtureBase"/>). It supplies only the
/// tenant-specific configuration, fakes, aggregate registration, query-cursor codec, and the
/// <c>/process</c> router map; all generic daprd/Aspire bootstrap lives in the EventStore platform
/// package.
/// </summary>
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
    protected override void ConfigureDomainConfiguration(ConfigurationManager configuration) {
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure domain service registration: system|tenants|v1 → self (commandapi).
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:AppId"] = AppId;
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:MethodName"] = "process";
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:TenantId"] = "system";
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Domain"] = "tenants";
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Version"] = "v1";
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:AppId"] = AppId;
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:MethodName"] = "process";
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:TenantId"] = "system";
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:Domain"] = TenantIdentity.GlobalAdministratorsDomain;
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:Version"] = "v1";

        // Configure pub/sub name for event publisher.
        configuration["EventStore:Publisher:PubSubName"] = PubSubName;
        configuration["EventStore:Publisher:TopicOverrides:global-administrators"] = "tenants.events";
        EventPublisher.TopicOverrides[TenantIdentity.GlobalAdministratorsDomain] = "tenants.events";

        // Speed up drain recovery for tests (default is 30s initial / 60s period).
        // Keeps DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers deterministic
        // within its 90s poll budget even if the first reminder tick races ClearFailure().
        configuration["EventStore:Drain:InitialDrainDelay"] = "00:00:05";
        configuration["EventStore:Drain:DrainPeriod"] = "00:00:05";
    }

    /// <inheritdoc/>
    protected override void ConfigureDomainServices(IServiceCollection services, IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register publisher fakes BEFORE AddEventStoreServer (TryAdd won't override these).
        _ = services.AddSingleton<IEventPublisher>(EventPublisher);
        _ = services.AddSingleton<IDeadLetterPublisher>(DeadLetterPublisher);

        // Register DAPR client and EventStore server infrastructure (actors, command routing, REAL domain service invoker).
        services.AddDaprClient();
        _ = services.AddEventStoreServer(configuration);
        _ = services.RemoveAll<ICommandStatusStore>();
        _ = services.AddSingleton<ICommandStatusStore>(CommandStatusStore);

        // Register real domain processors (TenantAggregate, GlobalAdministratorsAggregate). The keyed
        // IDomainProcessor registrations back the SDK /process router (DomainServiceRequestRouter).
        _ = services.AddEventStore(typeof(TenantAggregate).Assembly);

        _ = services.AddDataProtection()
            .SetApplicationName("Hexalith.Tenants.IntegrationTests");
        services.AddEventStoreQueryCursorCodec("Hexalith.Tenants.QueryCursor.v1");
    }

    /// <inheritdoc/>
    protected override void MapDomainEndpoints(WebApplication app) {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapPost("/process", async (
            DomainServiceRequest request,
            IServiceProvider serviceProvider,
            ILogger<TenantsDaprTestFixture> logger) => {
                try {
                    DomainServiceWireResult result = await DomainServiceRequestRouter.ProcessAsync(serviceProvider, request).ConfigureAwait(false);
                    return Microsoft.AspNetCore.Http.Results.Ok(result);
                }
                catch (Exception ex) {
                    string diagnostic = $"Domain processing failed for command type {request.Command.CommandType}.";
                    RecordProcessFailure(ex, diagnostic);
                    Console.Error.WriteLine($"[DAPR-TEST] /process 500. {diagnostic}");
                    logger.LogError("Domain processing failed for command type {CommandType}.", request.Command.CommandType);
                    return Microsoft.AspNetCore.Http.Results.Problem(
                        detail: diagnostic,
                        statusCode: 500);
                }
            });
    }
}
