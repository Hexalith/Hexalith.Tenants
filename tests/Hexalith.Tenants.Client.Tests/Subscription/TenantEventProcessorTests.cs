using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Subscription;

public class TenantEventProcessorTests
{
    private static IReadOnlyDictionary<string, Type> BuildRegistry()
    {
        return typeof(TenantCreated).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t);
    }

    private static TenantEventEnvelope CreateEnvelope<TEvent>(string messageId, TEvent @event)
        where TEvent : IEventPayload
    {
        return new TenantEventEnvelope(
            messageId,
            "acme",
            "system",
            typeof(TEvent).FullName!,
            1,
            DateTimeOffset.UtcNow,
            "corr-1",
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event));
    }

    private static (TenantEventProcessor Processor, InMemoryTenantProjectionStore Store, ServiceProvider Provider) CreateProcessor()
    {
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();

        var services = new ServiceCollection();
        services.AddSingleton<ITenantProjectionStore>(store);
        services.AddSingleton(handler);
        services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);
        services.AddSingleton<ITenantEventHandler<TenantUpdated>>(handler);
        services.AddSingleton<ITenantEventHandler<TenantDisabled>>(handler);
        services.AddSingleton<ITenantEventHandler<TenantEnabled>>(handler);
        services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(handler);
        services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(handler);
        services.AddSingleton<ITenantEventHandler<UserRoleChanged>>(handler);
        services.AddSingleton<ITenantEventHandler<TenantConfigurationSet>>(handler);
        services.AddSingleton<ITenantEventHandler<TenantConfigurationRemoved>>(handler);
        ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider,
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        return (processor, store, provider);
    }

    [Fact]
    public async Task ProcessAsync_KnownEventType_ReturnsProcessed()
    {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
            TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

            // Act
            TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

            // Assert
            result.ShouldBe(TenantEventProcessingResult.Processed);
        }
    }

    [Fact]
    public async Task ProcessAsync_KnownEventType_HandlerAppliesEvent()
    {
        // Arrange
        (TenantEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
            TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", "desc", DateTimeOffset.UtcNow));

            // Act
            await processor.ProcessAsync(envelope);

            // Assert
            TenantLocalState? state = await store.GetAsync("acme");
            state.ShouldNotBeNull();
            state.Name.ShouldBe("Acme Corp");
        }
    }

    [Fact]
    public async Task ProcessAsync_UnknownEventType_ReturnsSkippedUnknownEventType()
    {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
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
    public async Task ProcessAsync_DuplicateMessageId_ReturnsDuplicate()
    {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
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
    public async Task ProcessAsync_NoHandlersRegistered_ReturnsSkippedNoHandlers()
    {
        // Arrange
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        var processor = new TenantEventProcessor(
            provider,
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        // Act
        TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

        // Assert
        result.ShouldBe(TenantEventProcessingResult.SkippedNoHandlers);
    }

    [Fact]
    public async Task ProcessAsync_InvalidPayload_ReturnsFailedInvalidPayload()
    {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
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
    public async Task ProcessAsync_HandlerFailure_AllowsRetryWithSameMessageId()
    {
        // Arrange
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();
        var handler = new ThrowOnceHandler();
        var services = new ServiceCollection();
        services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        var processor = new TenantEventProcessor(
            provider,
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => processor.ProcessAsync(envelope));
        TenantEventProcessingResult retryResult = await processor.ProcessAsync(envelope);
        retryResult.ShouldBe(TenantEventProcessingResult.Processed);
    }

    [Fact]
    public async Task ProcessAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        // Arrange
        (TenantEventProcessor processor, _, ServiceProvider provider) = CreateProcessor();
        using (provider)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => processor.ProcessAsync(null!));
        }
    }

    [Fact]
    public async Task ProcessAsync_DispatchesToMultipleHandlers()
    {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var projectionHandler = new TenantProjectionEventHandler(store);
        var trackingHandler = new TrackingEventHandler();
        IReadOnlyDictionary<string, Type> registry = BuildRegistry();

        var services = new ServiceCollection();
        services.AddSingleton<ITenantEventHandler<TenantCreated>>(projectionHandler);
        services.AddSingleton<ITenantEventHandler<TenantCreated>>(trackingHandler);
        using ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider,
            registry,
            NullLogger<TenantEventProcessor>.Instance);

        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        TenantEventProcessingResult result = await processor.ProcessAsync(envelope);

        // Assert
        result.ShouldBe(TenantEventProcessingResult.Processed);
        TenantLocalState? state = await store.GetAsync("acme");
        state.ShouldNotBeNull();
        trackingHandler.HandledEvents.ShouldBe(1);
    }

    private sealed class ThrowOnceHandler : ITenantEventHandler<TenantCreated>
    {
        private int _attempts;

        public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default)
        {
            _attempts++;
            if (_attempts == 1)
            {
                throw new InvalidOperationException("Boom on first attempt.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TrackingEventHandler : ITenantEventHandler<TenantCreated>
    {
        public int HandledEvents { get; private set; }

        public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default)
        {
            HandledEvents++;
            return Task.CompletedTask;
        }
    }
}
