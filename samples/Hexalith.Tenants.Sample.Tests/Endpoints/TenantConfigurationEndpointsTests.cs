using System.Net.Http;
using System.Text.Json;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Endpoints;

public class TenantConfigurationEndpointsTests {
    private static IReadOnlyDictionary<string, Type> BuildRegistry() => typeof(TenantCreated).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t);

    private static EventStoreDomainEventEnvelope CreateEnvelope<TEvent>(string messageId, TEvent @event)
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

    private static (EventStoreDomainEventProcessor Processor, InMemoryTenantProjectionStore Store, ServiceProvider Provider) CreateProcessor() {
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);

        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore>(store);
        _ = services.AddSingleton(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<TenantCreated>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<TenantConfigurationSet>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<TenantConfigurationRemoved>>(handler);
        ServiceProvider provider = services.BuildServiceProvider();

        var processor = new EventStoreDomainEventProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            BuildRegistry(),
            NullLogger<EventStoreDomainEventProcessor>.Instance,
            "TenantId");

        return (processor, store, provider);
    }

    private static string SerializeResultValue(IResult result) {
        var valueResult = (IValueHttpResult)result;
        _ = valueResult.Value.ShouldNotBeNull();
        return JsonSerializer.Serialize(valueResult.Value);
    }

    [Fact]
    public async Task GetSampleConfigurationAsync_ReturnsOnlySampleNamespaceValues() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Configuration = {
                ["sample.theme"] = "blue",
                ["sample.mode"] = "compact",
                ["billing.plan"] = "enterprise",
            },
        });

        // Act
        IResult result = await TenantConfigurationEndpoints.GetSampleConfigurationAsync("acme", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"theme\":\"blue\"");
        json.ShouldContain("\"mode\":\"compact\"");
        json.ShouldNotContain("billing.plan");
        json.ShouldNotContain("enterprise");
    }

    [Fact]
    public void GetSampleConfigurationAsync_DependsOnProjectionStoreInsteadOfSynchronousTenantApiClient() {
        // Arrange
        Type[] parameterTypes = typeof(TenantConfigurationEndpoints)
            .GetMethod(nameof(TenantConfigurationEndpoints.GetSampleConfigurationAsync))!
            .GetParameters()
            .Select(static parameter => parameter.ParameterType)
            .ToArray();

        // Act
        string[] parameterTypeNames = parameterTypes
            .Select(static type => type.FullName ?? type.Name)
            .ToArray();

        // Assert
        parameterTypes.ShouldContain(typeof(ITenantProjectionStore));
        parameterTypes.ShouldNotContain(typeof(HttpClient));
        parameterTypeNames.ShouldNotContain("Dapr.Client.DaprClient");
    }

    [Fact]
    public async Task GetSampleConfigurationAsync_EventPipeline_AppliesUpdatesAndRepeatedRemoveDeterministically() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-set-1", new TenantConfigurationSet("acme", "sample.theme", "blue")));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-set-2", new TenantConfigurationSet("acme", "sample.theme", "green")));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-billing", new TenantConfigurationSet("acme", "billing.plan", "enterprise")));

            IResult updatedResult = await TenantConfigurationEndpoints.GetSampleConfigurationAsync("acme", store, CancellationToken.None);

            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-remove-1", new TenantConfigurationRemoved("acme", "sample.theme")));

            // Act
            EventStoreDomainEventProcessingResult repeatedRemove = await processor.ProcessAsync(
                CreateEnvelope("msg-remove-2", new TenantConfigurationRemoved("acme", "sample.theme")));
            IResult result = await TenantConfigurationEndpoints.GetSampleConfigurationAsync("acme", store, CancellationToken.None);

            // Assert
            ((IStatusCodeHttpResult)updatedResult).StatusCode.ShouldBe(200);
            string updatedJson = SerializeResultValue(updatedResult);
            updatedJson.ShouldContain("\"theme\":\"green\"");
            updatedJson.ShouldNotContain("\"theme\":\"blue\"");

            repeatedRemove.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldNotContain("theme");
            json.ShouldNotContain("green");
            json.ShouldNotContain("billing.plan");
            json.ShouldNotContain("enterprise");
        }
    }

    [Fact]
    public async Task GetSampleConfigurationAsync_UnknownTenant_ReturnsNotFound() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        IResult result = await TenantConfigurationEndpoints.GetSampleConfigurationAsync("unknown", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task GetSampleConfigurationAsync_NullStore_ThrowsArgumentNullException() =>
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => TenantConfigurationEndpoints.GetSampleConfigurationAsync("acme", null!, CancellationToken.None));

    [Fact]
    public async Task GetSampleConfigurationAsync_WhitespaceTenantId_ReturnsBadRequest() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        IResult result = await TenantConfigurationEndpoints.GetSampleConfigurationAsync(" ", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(400);
    }
}
