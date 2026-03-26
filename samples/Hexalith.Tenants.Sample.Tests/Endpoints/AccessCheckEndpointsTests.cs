using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;
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

        var services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore>(store);
        _ = services.AddSingleton(handler);
        _ = services.AddSingleton<ITenantEventHandler<TenantCreated>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(handler);
        _ = services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(handler);
        ServiceProvider provider = services.BuildServiceProvider();

        var processor = new TenantEventProcessor(
            provider,
            BuildRegistry(),
            NullLogger<TenantEventProcessor>.Instance);

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
    public async Task CheckAccessAsync_UnknownTenant_ReturnsNotFound() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        IResult result = await AccessCheckEndpoints.CheckAccessAsync("unknown", "user1", store, CancellationToken.None);

        // Assert
        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(404);
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
        (TenantEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            // Act
            TenantEventProcessingResult created = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            TenantEventProcessingResult added = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            created.ShouldBe(TenantEventProcessingResult.Processed);
            added.ShouldBe(TenantEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"granted\"");
            json.ShouldContain("\"Role\":\"TenantOwner\"");
        }
    }

    [Fact]
    public async Task CheckAccessAsync_UserRemovedEventPipeline_RevokesAccessFromProjection() {
        // Arrange
        (TenantEventProcessor processor, InMemoryTenantProjectionStore store, ServiceProvider provider) = CreateProcessor();
        using (provider) {
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-created", new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow)));
            _ = await processor.ProcessAsync(
                CreateEnvelope("msg-added", new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner)));

            // Act
            TenantEventProcessingResult removed = await processor.ProcessAsync(
                CreateEnvelope("msg-removed", new UserRemovedFromTenant("acme", "user1")));
            IResult result = await AccessCheckEndpoints.CheckAccessAsync("acme", "user1", store, CancellationToken.None);

            // Assert
            removed.ShouldBe(TenantEventProcessingResult.Processed);
            ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(200);
            string json = SerializeResultValue(result);
            json.ShouldContain("\"Access\":\"denied\"");
            json.ShouldContain("\"Reason\":\"User is not a member\"");
        }
    }
}
