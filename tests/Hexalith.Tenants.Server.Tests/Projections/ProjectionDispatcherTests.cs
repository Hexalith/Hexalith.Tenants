using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class ProjectionDispatcherTests {
    private static readonly JsonSerializerOptions _options = new() {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TenantTelemetry _telemetry = new(new EventStoreDomainDiagnostics("tenants"));

    [Fact]
    public async Task DispatchAsync_TenantsDomain_RoutesToTenantProjectionHandlerAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantReadModel>(null, null));
        store.GetAsync<TenantAuditReadModel>("statestore", "audit:tenant-1", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantAuditReadModel>(null, null));
        store.GetAsync<TenantIndexReadModel>("statestore", "projection:tenant-index:singleton", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantIndexReadModel>(null, null));
        store.TrySaveAsync("statestore", Arg.Any<string>(), Arg.Any<TenantReadModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        store.TrySaveAsync("statestore", Arg.Any<string>(), Arg.Any<TenantAuditReadModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        store.TrySaveAsync("statestore", Arg.Any<string>(), Arg.Any<TenantIndexReadModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        ProjectionRequest request = new(
            "system",
            "tenants",
            "tenant-1",
            [CreateEventDto(new TenantCreated("tenant-1", "Acme", null, DateTimeOffset.UtcNow))]);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        // Tenant handler writes the per-tenant projection key through a first-write-wins ETag save
        // (the loaded ETag is empty for a missing-state first write; the FirstWrite concurrency guard now
        // lives inside the platform DaprReadModelStore and is verified there).
        await store.Received(1).GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>());
        await store.Received(1).TrySaveAsync(
            "statestore",
            "projection:tenants:tenant-1",
            Arg.Any<TenantReadModel>(),
            string.Empty,
            Arg.Any<CancellationToken>());
        // Audit + index projections are written through the same guarded path.
        await store.Received(1).TrySaveAsync(
            "statestore",
            "audit:tenant-1",
            Arg.Any<TenantAuditReadModel>(),
            string.Empty,
            Arg.Any<CancellationToken>());
        await store.Received(1).TrySaveAsync(
            "statestore",
            "projection:tenant-index:singleton",
            Arg.Any<TenantIndexReadModel>(),
            string.Empty,
            Arg.Any<CancellationToken>());
        // ...and the global-admin singleton must NOT be touched (the tenant path never uses SaveAsync).
        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
        _ = result.ShouldBeOfType<Ok<ProjectionResponse>>();
    }

    [Fact]
    public async Task DispatchAsync_GlobalAdministratorsDomain_RoutesToGlobalAdminHandlerAndWritesSingletonAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new(
            "system",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        await store.Received(1).SaveAsync(
            "statestore",
            "projection:global-administrators:singleton",
            Arg.Is<GlobalAdministratorReadModel>(m => m != null && m.Administrators.Contains("admin-user")),
            Arg.Any<CancellationToken>());
        // No tenant-side keys should have been touched.
        await store.DidNotReceive().TrySaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantReadModel>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().TrySaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantIndexReadModel>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        Ok<ProjectionResponse> ok = result.ShouldBeOfType<Ok<ProjectionResponse>>();
        ok.Value!.ProjectionType.ShouldBe("global-administrators");
    }

    [Theory]
    [InlineData("tenant-a", "global-administrators")]
    [InlineData("system", "tenant-a")]
    public async Task DispatchAsync_GlobalAdministratorsDomain_WithInvalidIdentity_Returns400AndDoesNotWriteStateAsync(
        string tenant,
        string aggregateId) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new(
            tenant,
            "global-administrators",
            aggregateId,
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
        ProblemHttpResult problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GlobalAdministratorProjectionHandler_InvalidIdentity_ThrowsBeforeStateWriteAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new(
            "tenant-a",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        _ = await Should.ThrowAsync<ArgumentException>(
            () => new GlobalAdministratorProjectionHandler(store).ProjectAsync(request));

        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("counter")]
    [InlineData("orders")]
    [InlineData("")]
    [InlineData("Tenants")] // case-sensitive: must not fall through to tenants
    public async Task DispatchAsync_UnknownDomain_Returns400AndDoesNotWriteStateAsync(string domain) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new("system", domain, "any", []);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        // No state writes for unsupported domains.
        await store.DidNotReceive().SaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<GlobalAdministratorReadModel>(),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().TrySaveAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TenantReadModel>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Response is RFC 7807 ProblemDetails with 400 status.
        ProblemHttpResult problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task DispatchAsync_NullRequest_ThrowsAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionDispatcher dispatcher = new(store, _telemetry);

        _ = await Should.ThrowAsync<ArgumentNullException>(
            () => dispatcher.DispatchAsync(null!));
    }

    private static ProjectionEventDto CreateEventDto(object @event) {
        string typeName = @event switch {
            TenantCreated => "Hexalith.Tenants.Contracts.Events.TenantCreated",
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
            MessageId: "evt-test",
            UserId: "actor-test");
    }
}
