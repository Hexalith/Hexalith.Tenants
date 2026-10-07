using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

/// <summary>
/// EventStore Story 5.5: a wire administrator assertion reaching the Tenants domain service is honored only when the
/// acting user is a current member of the global-administrators read model; an unavailable store fails closed.
/// </summary>
public sealed class TenantsGlobalAdministratorVerifierTests {
    [Fact]
    public async Task Current_member_is_verified() {
        IReadModelStore store = Store("admin-user");

        bool verified = await new TenantsGlobalAdministratorVerifier(store)
            .IsCurrentGlobalAdministratorAsync(Claim("admin-user"), CancellationToken.None);

        verified.ShouldBeTrue();
    }

    [Theory]
    [InlineData("someone-else")]
    [InlineData("")]
    public async Task Non_member_is_not_verified(string userId) {
        IReadModelStore store = Store("admin-user");

        bool verified = await new TenantsGlobalAdministratorVerifier(store)
            .IsCurrentGlobalAdministratorAsync(Claim(userId), CancellationToken.None);

        verified.ShouldBeFalse();
    }

    [Fact]
    public async Task Missing_projection_is_not_verified() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(null, null)));

        bool verified = await new TenantsGlobalAdministratorVerifier(store)
            .IsCurrentGlobalAdministratorAsync(Claim("admin-user"), CancellationToken.None);

        verified.ShouldBeFalse();
    }

    [Fact]
    public async Task Unavailable_store_fails_closed() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        _ = store.GetAsync<GlobalAdministratorReadModel>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("state store unavailable"));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => new TenantsGlobalAdministratorVerifier(store)
            .IsCurrentGlobalAdministratorAsync(Claim("admin-user"), CancellationToken.None));
    }

    private static DomainServiceAdministratorClaim Claim(string userId)
        => new("system", "tenants", userId, "correlation-1");

    private static IReadModelStore Store(params string[] administrators) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        var model = new GlobalAdministratorReadModel {
            Administrators = administrators.ToHashSet(StringComparer.Ordinal),
        };
        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, "admin-etag")));
        return store;
    }
}
