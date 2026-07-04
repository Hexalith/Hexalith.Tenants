using System.Collections.Concurrent;

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
using Microsoft.Extensions.Logging;

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

    /// <summary>Gets the isolated aggregate actor type name registered by this fixture run.</summary>
    public string AggregateActorTypeName { get; } = $"TenantsAggregateActorTests{Guid.NewGuid():N}";

    private readonly ConcurrentQueue<string> _supportDiagnostics = new();

    /// <summary>Formats recent support-safe warning/error diagnostics emitted by the test host.</summary>
    public string FormatRecentDiagnostics() {
        string[] diagnostics = [.. _supportDiagnostics.TakeLast(12)];
        return diagnostics.Length == 0
            ? "<none>"
            : string.Join(" | ", diagnostics);
    }

    /// <inheritdoc/>
    protected override void ConfigureDomain(WebApplicationBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Logging.AddProvider(new SupportSafeDiagnosticLogProvider(_supportDiagnostics));

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
        builder.Configuration["EventStore:Actors:AggregateActorTypeName"] = AggregateActorTypeName;

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

    private sealed class SupportSafeDiagnosticLogProvider(ConcurrentQueue<string> sink) : ILoggerProvider {
        public ILogger CreateLogger(string categoryName) => new SupportSafeDiagnosticLogger(categoryName, sink);

        public void Dispose() {
        }
    }

    private sealed class SupportSafeDiagnosticLogger(string categoryName, ConcurrentQueue<string> sink) : ILogger {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (!IsEnabled(logLevel)) {
                return;
            }

            string diagnostic = BuildDiagnostic(logLevel, eventId, state, exception);
            sink.Enqueue(diagnostic);
            while (sink.Count > 64) {
                _ = sink.TryDequeue(out _);
            }
        }

        private string BuildDiagnostic<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception) {
            var parts = new List<string> {
                $"{logLevel}:{categoryName}:{eventId.Id}:{eventId.Name ?? "<unnamed>"}",
            };

            if (exception is not null) {
                parts.Add($"ExceptionType={exception.GetType().FullName}");
            }

            if (state is IEnumerable<KeyValuePair<string, object?>> structured) {
                foreach (KeyValuePair<string, object?> item in structured) {
                    if (IsSafeDiagnosticKey(item.Key) && item.Value is not null) {
                        parts.Add($"{item.Key}={item.Value}");
                    }
                }
            }

            return string.Join(";", parts);
        }

        private static bool IsSafeDiagnosticKey(string key)
            => key is "Stage"
                or "FailureStage"
                or "ExceptionType"
                or "ErrorMessage"
                or "SafeDiagnostic"
                or "ReasonCode"
                or "Status"
                or "CommandType";
    }
}
