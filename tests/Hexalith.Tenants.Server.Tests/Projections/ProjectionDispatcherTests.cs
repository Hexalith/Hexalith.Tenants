using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class ProjectionDispatcherTests {
    private static readonly JsonSerializerOptions _options = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task DispatchAsync_TenantsDomain_RoutesToTenantProjectionHandlerAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAndETagAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1")
            .Returns(Task.FromResult((default(TenantReadModel)!, string.Empty)));
        _ = daprClient.TrySaveStateAsync(
            "statestore",
            "projection:tenants:tenant-1",
            Arg.Any<TenantReadModel>(),
            Arg.Any<string>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _ = daprClient.GetStateAndETagAsync<TenantAuditReadModel>("statestore", "audit:tenant-1")
            .Returns(Task.FromResult((default(TenantAuditReadModel)!, string.Empty)));
        _ = daprClient.TrySaveStateAsync(
            "statestore",
            "audit:tenant-1",
            Arg.Any<TenantAuditReadModel>(),
            Arg.Any<string>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _ = daprClient.GetStateAndETagAsync<TenantIndexReadModel>("statestore", "projection:tenant-index:singleton")
            .Returns(Task.FromResult((default(TenantIndexReadModel)!, string.Empty)));
        _ = daprClient.TrySaveStateAsync(
            "statestore",
            "projection:tenant-index:singleton",
            Arg.Any<TenantIndexReadModel>(),
            Arg.Any<string>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        ProjectionRequest request = new(
            "system",
            "tenants",
            "tenant-1",
            [CreateEventDto(new TenantCreated("tenant-1", "Acme", null, DateTimeOffset.UtcNow))]);

        IResult result = await new ProjectionDispatcher(daprClient).DispatchAsync(request);

        // Tenant handler writes the per-tenant projection key through a guarded ETag save.
        await daprClient.Received(1).GetStateAndETagAsync<TenantReadModel>(
            "statestore",
            "projection:tenants:tenant-1");
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "projection:tenants:tenant-1",
            Arg.Any<TenantReadModel>(),
            string.Empty,
            Arg.Is<Dapr.Client.StateOptions>(o => o != null && o.Concurrency == ConcurrencyMode.FirstWrite),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "projection:tenants:tenant-1",
            Arg.Any<TenantReadModel>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        // Audit projection writes are also guarded so access history cannot be overwritten.
        // Pin the ETag (empty for a missing-state first write) and ConcurrencyMode so a future
        // weakening of the guarded save — e.g. dropping FirstWrite or omitting the loaded ETag —
        // fails this assertion instead of silently regressing AC1.
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "audit:tenant-1",
            Arg.Any<TenantAuditReadModel>(),
            string.Empty,
            Arg.Is<Dapr.Client.StateOptions>(o => o != null && o.Concurrency == ConcurrencyMode.FirstWrite),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "projection:tenant-index:singleton",
            Arg.Any<TenantIndexReadModel>(),
            string.Empty,
            Arg.Is<Dapr.Client.StateOptions>(o => o != null && o.Concurrency == ConcurrencyMode.FirstWrite),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "projection:tenant-index:singleton",
            Arg.Any<TenantIndexReadModel>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        // ...and the global-admin singleton must NOT be touched.
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "projection:global-administrators:singleton",
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        _ = result.ShouldBeOfType<Ok<ProjectionResponse>>();
    }

    [Fact]
    public async Task DispatchAsync_GlobalAdministratorsDomain_RoutesToGlobalAdminHandlerAndWritesSingletonAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ProjectionRequest request = new(
            "system",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        IResult result = await new ProjectionDispatcher(daprClient).DispatchAsync(request);

        await daprClient.Received(1).SaveStateAsync(
            "statestore",
            "projection:global-administrators:singleton",
            Arg.Is<GlobalAdministratorReadModel>(m => m != null && m.Administrators.Contains("admin-user")),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        // No tenant-side keys should have been touched.
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "projection:tenants:global-administrators",
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "projection:tenant-index:singleton",
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
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
        DaprClient daprClient = Substitute.For<DaprClient>();
        ProjectionRequest request = new(
            tenant,
            "global-administrators",
            aggregateId,
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        IResult result = await new ProjectionDispatcher(daprClient).DispatchAsync(request);

        await daprClient.DidNotReceive().SaveStateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
        ProblemHttpResult problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GlobalAdministratorProjectionHandler_InvalidIdentity_ThrowsBeforeStateWriteAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ProjectionRequest request = new(
            "tenant-a",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        _ = await Should.ThrowAsync<ArgumentException>(
            () => new GlobalAdministratorProjectionHandler(daprClient).ProjectAsync(request));

        await daprClient.DidNotReceive().SaveStateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("counter")]
    [InlineData("orders")]
    [InlineData("")]
    [InlineData("Tenants")] // case-sensitive: must not fall through to tenants
    public async Task DispatchAsync_UnknownDomain_Returns400AndDoesNotWriteStateAsync(string domain) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ProjectionRequest request = new("system", domain, "any", []);

        IResult result = await new ProjectionDispatcher(daprClient).DispatchAsync(request);

        // No DAPR state writes for unsupported domains.
        await daprClient.DidNotReceive().SaveStateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());

        // Response is RFC 7807 ProblemDetails with 400 status.
        ProblemHttpResult problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task DispatchAsync_NullRequest_ThrowsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ProjectionDispatcher dispatcher = new(daprClient);

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
