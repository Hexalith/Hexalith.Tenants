using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Subscription;

public class TenantEventProcessorTests {
    private static IReadOnlyDictionary<string, Type> BuildRegistry() => typeof(TenantCreated).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t);

    private static TenantEventEnvelope CreateEnvelope<TEvent>(string messageId, TEvent @event)
        where TEvent : IEventPayload => new(
            messageId,
            "acme",
            "system",
            typeof(TEvent).FullName!,
            1,
            DateTimeOffset.UtcNow,
            "corr-1",
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event));

    private static (TenantEventProcessor Processor, InMemoryTenantProjectionStore Store, ServiceProvider Provider) CreateProcessor() {
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();

        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore>(store);
        _ = services.AddSingleton(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantUpdated>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantDisabled>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantEnabled>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<UserRoleChanged>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantConfigurationSet>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantConfigurationRemoved>>(handler);
        ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        return (processor, store, provider);
    }

    [Fact]
    public async Task ProcessAsync_KnownEventType_ReturnsProcessed() {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

            // Act
            TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

            // Assert
            result.ShouldBe(TenantEventProcessingResult.Processed);
        }
    }

    [Fact]
    public async Task ProcessAsync_KnownEventType_HandlerAppliesEvent() {
        // Arrange
        (TenantEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", "desc", DateTimeOffset.UtcNow));

            // Act
            _ = await processor.ProcessAsync(envelope);

            // Assert
            TenantLocalState? state = await store.GetAsync("acme");
            _ = state.ShouldNotBeNull();
            state.Name.ShouldBe("Acme Corp");
        }
    }

    [Fact]
    public async Task ProcessAsync_UnknownEventType_ReturnsSkippedUnknownEventType() {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            var envelope = new TenantEventEnvelope(
                "msg-1",
                "acme",
                "system",
                "Some.Unknown.EventType",
                1,
                DateTimeOffset.UtcNow,
                "corr-1",
                "json",
                []);

            // Act
            TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

            // Assert
            result.ShouldBe(TenantEventProcessingResult.SkippedUnknownEventType);
        }
    }

    [Fact]
    public async Task ProcessAsync_DuplicateMessageId_ReturnsDuplicate() {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

            // Act
            TenantEventProcessingResult first = await processor.ProcessAsync(envelope);
            TenantEventProcessingResult second = await processor.ProcessAsync(envelope);

            // Assert
            first.ShouldBe(TenantEventProcessingResult.Processed);
            second.ShouldBe(TenantEventProcessingResult.Duplicate);
        }
    }

    [Fact]
    public async Task ProcessAsync_DuplicateMessageId_DoesNotSaveProjectionTwice() {
        // Arrange
        var store = new CountingTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();

        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore>(store);
        _ = services.AddSingleton(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);
        using ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        TenantEventProcessingResult first = await processor.ProcessAsync(envelope);
        TenantEventProcessingResult second = await processor.ProcessAsync(envelope);

        // Assert
        first.ShouldBe(TenantEventProcessingResult.Processed);
        second.ShouldBe(TenantEventProcessingResult.Duplicate);
        store.SaveCount.ShouldBe(1);
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task ProcessAsync_RepresentativeEventSequence_ProducesDeterministicProjectionState() {
        // Arrange
        (TenantEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            // Act
            TenantEventProcessingResult created = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow)));
            TenantEventProcessingResult updated = await processor.ProcessAsync(
                CreateEnvelope("msg-updated", new TenantUpdated("acme", "Acme International", "Updated", DateTimeOffset.UtcNow)));
            TenantEventProcessingResult added = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantReader)));
            TenantEventProcessingResult changed = await processor.ProcessAsync(
                CreateEnvelope("msg-changed", new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantOwner)));
            TenantEventProcessingResult disabled = await processor.ProcessAsync(
                CreateEnvelope("msg-disabled", new TenantDisabled("acme", DateTimeOffset.UtcNow)));
            TenantEventProcessingResult enabled = await processor.ProcessAsync(
                CreateEnvelope("msg-enabled", new TenantEnabled("acme", DateTimeOffset.UtcNow)));
            TenantEventProcessingResult removed = await processor.ProcessAsync(
                CreateEnvelope("msg-removed", new UserRemovedFromTenant("acme", "user1")));

            // Assert
            created.ShouldBe(TenantEventProcessingResult.Processed);
            updated.ShouldBe(TenantEventProcessingResult.Processed);
            added.ShouldBe(TenantEventProcessingResult.Processed);
            changed.ShouldBe(TenantEventProcessingResult.Processed);
            disabled.ShouldBe(TenantEventProcessingResult.Processed);
            enabled.ShouldBe(TenantEventProcessingResult.Processed);
            removed.ShouldBe(TenantEventProcessingResult.Processed);

            TenantLocalState? state = await store.GetAsync("acme");
            _ = state.ShouldNotBeNull();
            state.Name.ShouldBe("Acme International");
            state.Description.ShouldBe("Updated");
            state.Status.ShouldBe(TenantStatus.Active);
            state.Members.ShouldNotContainKey("user1");
        }
    }

    [Fact]
    public async Task ProcessAsync_NoHandlersRegistered_ReturnsSkippedNoHandlers() {
        // Arrange
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        var processor = new TenantEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        // Act
        TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

        // Assert
        result.ShouldBe(TenantEventProcessingResult.SkippedNoHandlers);
    }

    [Fact]
    public async Task ProcessAsync_InvalidPayload_ReturnsFailedInvalidPayload() {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            var envelope = new TenantEventEnvelope(
                "msg-1",
                "acme",
                "system",
                typeof(TenantCreated).FullName!,
                1,
                DateTimeOffset.UtcNow,
                "corr-1",
                "json",
                [1, 2, 3]);

            // Act
            TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

            // Assert
            result.ShouldBe(TenantEventProcessingResult.FailedInvalidPayload);
        }
    }

    [Fact]
    public async Task ProcessAsync_HandlerFailure_AllowsRetryWithSameMessageId() {
        // Arrange
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();
        var handler = new ThrowOnceHandler();
        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        var processor = new TenantEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(() => processor.ProcessAsync(envelope));
        TenantEventProcessingResult retryResult = await processor.ProcessAsync(envelope);
        retryResult.ShouldBe(TenantEventProcessingResult.Processed);
    }

    [Fact]
    public async Task ProcessAsync_NullEnvelope_ThrowsArgumentNullException() {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            // Act & Assert
            _ = await Should.ThrowAsync<ArgumentNullException>(() => processor.ProcessAsync(null!));
        }
    }

    [Fact]
    public async Task ProcessAsync_DispatchesToMultipleHandlers() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var projectionHandler = new TenantProjectionEventHandler(store);
        var trackingHandler = new TrackingEventHandler();
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();

        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(projectionHandler);
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(trackingHandler);
        using ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

        // Assert
        result.ShouldBe(TenantEventProcessingResult.Processed);
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        trackingHandler.HandledEvents.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessAsync_DispatchesSelectedCustomHandlersOnlyForMatchingEventType() {
        // Arrange
        var sink = new TrackingSink();
        var services = new ServiceCollection();
        _ = services.AddSingleton(sink);
        _ = services.AddSingleton<ILogger<TenantEventProcessor>>(NullLogger<TenantEventProcessor>.Instance);
        _ = services
            .AddHexalithTenants()
            .AddTenantEventHandler<UserAddedToTenant, SelectedUserAddedHandler>();

        using ServiceProvider provider = services.BuildServiceProvider();
        TenantEventProcessor processor = provider.GetRequiredService<TenantEventProcessor>();

        TenantEventEnvelope unrelatedEnvelope = CreateEnvelope("msg-1", new TenantDisabled("acme", DateTimeOffset.UtcNow));
        TenantEventEnvelope selectedEnvelope = CreateEnvelope("msg-2", new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        // Act
        TenantEventProcessingResult unrelatedResult = await processor.ProcessAsync(unrelatedEnvelope);
        TenantEventProcessingResult selectedResult = await processor.ProcessAsync(selectedEnvelope);

        // Assert
        unrelatedResult.ShouldBe(TenantEventProcessingResult.Processed);
        selectedResult.ShouldBe(TenantEventProcessingResult.Processed);
        sink.HandledEventTypeNames.ShouldBe([nameof(UserAddedToTenant)]);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesCustomHandlersThroughEventScope() {
        // Arrange
        var sink = new TrackingSink();
        var services = new ServiceCollection();
        _ = services.AddSingleton(sink);
        _ = services.AddSingleton<ILogger<TenantEventProcessor>>(NullLogger<TenantEventProcessor>.Instance);
        _ = services.AddScoped<ScopedTenantEventDependency>();
        _ = services
            .AddHexalithTenants()
            .AddTenantEventHandler<UserAddedToTenant, ScopedDependencyHandler>();

        using ServiceProvider provider = services.BuildServiceProvider();
        TenantEventProcessor processor = provider.GetRequiredService<TenantEventProcessor>();
        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        // Act
        TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

        // Assert
        result.ShouldBe(TenantEventProcessingResult.Processed);
        sink.DependencyIds.Count.ShouldBe(1);
        sink.DependencyIds[0].ShouldNotBe(Guid.Empty);
    }

    private sealed class ThrowOnceHandler : ITenantEventHandler<TenantCreated> {
        private int _attempts;

        public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default) {
            _attempts++;
            if (_attempts == 1) {
                throw new InvalidOperationException("Boom on first attempt.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TrackingEventHandler : ITenantEventHandler<TenantCreated> {
        public int HandledEvents { get; private set; }

        public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default) {
            HandledEvents++;
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingSink {
        public List<Guid> DependencyIds { get; } = [];

        public List<string> HandledEventTypeNames { get; } = [];
    }

    private sealed class ScopedTenantEventDependency {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class CountingTenantProjectionStore : ITenantProjectionStore {
        private readonly InMemoryTenantProjectionStore _inner = new();

        public int SaveCount { get; private set; }

        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => _inner.GetAsync(tenantId, cancellationToken);

        public async Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default) {
            SaveCount++;
            await _inner.SaveAsync(state, cancellationToken);
        }
    }

    private sealed class SelectedUserAddedHandler : ITenantEventHandler<UserAddedToTenant> {
        private readonly TrackingSink _sink;

        public SelectedUserAddedHandler(TrackingSink sink) {
            ArgumentNullException.ThrowIfNull(sink);
            _sink = sink;
        }

        public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default) {
            _sink.HandledEventTypeNames.Add(nameof(UserAddedToTenant));
            return Task.CompletedTask;
        }
    }

    private sealed class ScopedDependencyHandler : ITenantEventHandler<UserAddedToTenant> {
        private readonly ScopedTenantEventDependency _dependency;
        private readonly TrackingSink _sink;

        public ScopedDependencyHandler(ScopedTenantEventDependency dependency, TrackingSink sink) {
            ArgumentNullException.ThrowIfNull(dependency);
            ArgumentNullException.ThrowIfNull(sink);
            _dependency = dependency;
            _sink = sink;
        }

        public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default) {
            _sink.DependencyIds.Add(_dependency.Id);
            return Task.CompletedTask;
        }
    }
}
