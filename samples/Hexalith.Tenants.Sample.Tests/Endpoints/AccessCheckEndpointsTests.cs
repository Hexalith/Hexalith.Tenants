using System.Net.Http;
using System.Text.Json;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Endpoints;

public class AccessCheckEndpointsTests {
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
        _ = services.AddSingleton<IEventStoreDomainEventHandler<TenantDisabled>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<TenantEnabled>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<UserAddedToTenant>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<UserRemovedFromTenant>>(handler);
        _ = services.AddSingleton<IEventStoreDomainEventHandler<UserRoleChanged>>(handler);
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
    public async Task CheckAccessAsync_MemberWithRole_ReturnsGranted() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Active,
            Members = { ["user1"] = TenantRole.TenantOwner },
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"granted\"");
        json.ShouldContain("\"Role\":\"TenantOwner\"");
    }

    [Fact]
    public async Task CheckAccessAsync_NonMember_ReturnsDenied() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Active,
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "unknown-user", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"denied\"");
        json.ShouldContain("\"Reason\":\"User is not a member\"");
    }

    [Fact]
    public async Task CheckAccessAsync_DisabledTenant_ReturnsDenied() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Disabled,
            Members = { ["user1"] = TenantRole.TenantOwner },
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"denied\"");
        json.ShouldContain("\"Reason\":\"Tenant is disabled\"");
    }

    [Fact]
    public async Task CheckAccessAsync_UnknownTenantStatus_ReturnsDenied() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Unknown,
            Members = { ["user1"] = TenantRole.TenantOwner },
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"denied\"");
        json.ShouldContain("\"Reason\":\"Tenant is not active\"");
    }

    [Fact]
    public async Task CheckAccessAsync_UnknownRole_ReturnsDenied() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Active,
            Members = { ["user1"] = TenantRole.Unknown },
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"denied\"");
        json.ShouldContain("\"Reason\":\"User role is not authorized\"");
    }

    [Fact]
    public async Task CheckAccessAsync_OutOfRangeRole_ReturnsDenied() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            Status = TenantStatus.Active,
            Members = { ["user1"] = (TenantRole)999 },
        });

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
        string json = SerializeResultValue(result);
        json.ShouldContain("\"Access\":\"denied\"");
        json.ShouldContain("\"Reason\":\"User role is not authorized\"");
    }

    [Fact]
    public async Task CheckAccessAsync_UnknownTenant_ReturnsNotFound() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("unknown", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(404);
    }

    [Fact]
    public void CheckAccessAsync_DependsOnProjectionStoreInsteadOfSynchronousTenantApiClient() {
        // Arrange
        Type[] parameterTypes = typeof(AccessCheckEndpoints)
            .GetMethod(nameof(AccessCheckEndpoints.CheckAccessAsync))!
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
    public async Task CheckAccessAsync_NullStore_ThrowsArgumentNullException() =>
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => AccessCheckEndpoints.CheckAccessAsync("acme", "user1", null!, CancellationToken.None));

    [Theory]
    [InlineData(" ", "user1")]
    [InlineData("acme", " ")]
    public async Task CheckAccessAsync_WhitespaceIdentifiers_ReturnsBadRequest(string tenantId, string userId) {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync(tenantId, userId, store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CheckAccessAsync_UserAddedEventPipeline_GrantsAccessFromProjection() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            // Act
            EventStoreDomainEventProcessingResult created = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            EventStoreDomainEventProcessingResult added = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            created.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            added.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"granted\"");
            json.ShouldContain("\"Role\":\"TenantOwner\"");
        }
    }

    [Fact]
    public async Task CheckAccessAsync_UserRemovedEventPipeline_RevokesAccessFromProjection() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));

            // Act
            EventStoreDomainEventProcessingResult removed = await processor.ProcessAsync(
                CreateEnvelope("msg-removed", new UserRemovedFromTenant("acme", "user1")));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            removed.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"denied\"");
            json.ShouldContain("\"Reason\":\"User is not a member\"");
        }
    }

    [Fact]
    public async Task CheckAccessAsync_UserRoleChangedEventPipeline_UsesUpdatedProjectedRole() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantReader)));

            // Act
            EventStoreDomainEventProcessingResult changed = await processor.ProcessAsync(
                CreateEnvelope("msg-changed", new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantContributor)));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            changed.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"granted\"");
            json.ShouldContain("\"Role\":\"TenantContributor\"");
        }
    }

    [Fact]
    public async Task CheckAccessAsync_RepeatedUserRemovedEventPipeline_RemainsDeniedWithoutError() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-removed-1", new UserRemovedFromTenant("acme", "user1")));

            // Act
            EventStoreDomainEventProcessingResult repeatedRemove = await processor.ProcessAsync(
                CreateEnvelope("msg-removed-2", new UserRemovedFromTenant("acme", "user1")));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            repeatedRemove.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"denied\"");
            json.ShouldContain("\"Reason\":\"User is not a member\"");
        }
    }

    [Fact]
    public async Task CheckAccessAsync_DisableAndEnableEventPipeline_DeniesThenRestoresAccessFromProjection() {
        // Arrange
        (EventStoreDomainEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));

            // Act
            EventStoreDomainEventProcessingResult disabled = await processor.ProcessAsync(
                CreateEnvelope("msg-disabled", new TenantDisabled("acme", DateTimeOffset.UtcNow)));
            IResult disabledResult = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);
            EventStoreDomainEventProcessingResult enabled = await processor.ProcessAsync(
                CreateEnvelope("msg-enabled", new TenantEnabled("acme", DateTimeOffset.UtcNow)));
            IResult enabledResult = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            disabled.ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            enabled.ShouldBe(EventStoreDomainEventProcessingResult.Processed);

            ((IStatusCodeHttpResult)disabledResult).StatusCode.ShouldBe(200);
            string disabledJson = SerializeResultValue(disabledResult);
            disabledJson.ShouldContain("\"Access\":\"denied\"");
            disabledJson.ShouldContain("\"Reason\":\"Tenant is disabled\"");

            ((IStatusCodeHttpResult)enabledResult).StatusCode.ShouldBe(200);
            string enabledJson = SerializeResultValue(enabledResult);
            enabledJson.ShouldContain("\"Access\":\"granted\"");
            enabledJson.ShouldContain("\"Role\":\"TenantOwner\"");
        }
    }
}
