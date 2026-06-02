using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class GlobalAdministratorProjectionHandlerTests {
    private const string StateStoreName = GlobalAdministratorProjectionHandler.StateStoreName;
    private const string SingletonKey = GlobalAdministratorProjectionHandler.GlobalAdministratorsProjectionKey;
    private const string ForbiddenTenantKey = "projection:tenants:global-administrators";

    private static readonly JsonSerializerOptions _options = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ProjectAsync_GlobalAdministratorSet_WritesSingletonKeyWithUserAddedAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-user"));

        ProjectionResponse response = await new GlobalAdministratorProjectionHandler(store)
            .ProjectAsync(request);

        response.ProjectionType.ShouldBe("global-administrators");
        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m => m != null && m.Administrators.Contains("admin-user") && m.Administrators.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_GlobalAdministratorEvents_WriteSystemAuditStateAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-1", "bootstrapper"),
            new GlobalAdministratorRemoved("system", "admin-1", "admin-2"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            "audit:system",
            Arg.Is<TenantAuditReadModel>(m =>
                m != null
                && m.Entries.Count == 2
                && m.Entries[0].EventId == "evt-1"
                && m.Entries[0].EventType == nameof(GlobalAdministratorSet)
                && m.Entries[0].ActorId == "actor-1"
                && m.Entries[0].TenantId == "system"
                && m.Entries[0].Target == "admin-1"
                && m.Entries[1].EventType == nameof(GlobalAdministratorRemoved)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_MultipleAdministratorsSet_WritesAllAdministratorsAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-1"),
            new GlobalAdministratorSet("system", "admin-2"),
            new GlobalAdministratorSet("system", "admin-3"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m =>
                m != null
                && m.Administrators.Count == 3
                && m.Administrators.Contains("admin-1")
                && m.Administrators.Contains("admin-2")
                && m.Administrators.Contains("admin-3")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_GlobalAdministratorRemoved_WritesSingletonKeyWithUserRemovedAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-1"),
            new GlobalAdministratorSet("system", "admin-2"),
            new GlobalAdministratorRemoved("system", "admin-1"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m =>
                m != null
                && m.Administrators.Count == 1
                && m.Administrators.Contains("admin-2")
                && !m.Administrators.Contains("admin-1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_EmptyEventList_WritesEmptySingletonAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new("system", "global-administrators", "global-administrators", []);

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m => m != null && m.Administrators.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_RemoveBeforeSet_LeavesAdministratorsEmptyAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorRemoved("system", "ghost"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m => m != null && m.Administrators.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_DoesNotWriteForbiddenTenantKeyAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-user"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            ForbiddenTenantKey,
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_DoesNotTouchTenantIndexProjectionAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-user"));

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            "projection:tenant-index:singleton",
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_NullEventEntries_AreSkippedAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionEventDto[] events = [
            CreateEventDto(new GlobalAdministratorSet("system", "admin-1")),
            null!,
            CreateEventDto(new GlobalAdministratorSet("system", "admin-2")),
        ];
        ProjectionRequest request = new("system", "global-administrators", "global-administrators", events);

        _ = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(request);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Is<GlobalAdministratorReadModel>(m =>
                m != null
                && m.Administrators.Count == 2
                && m.Administrators.Contains("admin-1")
                && m.Administrators.Contains("admin-2")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_WithPreCancelledTokenThrowsBeforeSaveAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-user"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => new GlobalAdministratorProjectionHandler(store).ProjectAsync(request, cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectAsync_PassesCancellationTokenToSaveStateBoundaryAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateRequest(
            new GlobalAdministratorSet("system", "admin-user"));
        using var cancellation = new CancellationTokenSource();

        _ = await new GlobalAdministratorProjectionHandler(store)
            .ProjectAsync(request, cancellation.Token);

        await store.Received(1).SaveAsync(
            StateStoreName,
            SingletonKey,
            Arg.Any<GlobalAdministratorReadModel>(),
            cancellation.Token);
    }

    private static ProjectionRequest CreateRequest(params object[] events) {
        ProjectionEventDto[] dtos = [.. events.Select((e, index) => CreateEventDto(e, $"evt-{index + 1}"))];
        return new ProjectionRequest("system", "global-administrators", "global-administrators", dtos);
    }

    private static ProjectionEventDto CreateEventDto(object @event, string messageId = "evt-1") {
        string typeName = @event switch {
            GlobalAdministratorSet => "Hexalith.Tenants.Contracts.Events.GlobalAdministratorSet",
            GlobalAdministratorRemoved => "Hexalith.Tenants.Contracts.Events.GlobalAdministratorRemoved",
            _ => @event.GetType().FullName ?? @event.GetType().Name,
        };
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, _options));
        return new ProjectionEventDto(
            typeName,
            payload,
            "json",
            1L,
            DateTimeOffset.UtcNow,
            "corr-1",
            messageId,
            "actor-1");
    }
}
