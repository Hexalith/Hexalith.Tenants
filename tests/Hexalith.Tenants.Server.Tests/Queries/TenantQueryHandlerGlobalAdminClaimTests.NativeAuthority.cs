using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed partial class TenantQueryHandlerGlobalAdminClaimTests
{
    [Theory]
    [InlineData("list")]
    [InlineData("detail")]
    [InlineData("users")]
    [InlineData("memberships")]
    [InlineData("audit")]
    [InlineData("administrators")]
    public async Task RemovedNativeAdministratorCannotUseAnyStaleProjectionGrant(string gate)
    {
        var store = Substitute.For<IReadModelStore>();
        SeedGlobalAdministrators(store, "admin"); SeedTenant(store); SeedTenantIndex(store);
        var source = GlobalAdministratorSourceFixture.Source(revoked: true);
        var authority = GlobalAdministratorSourceFixture.Authority(source, out _, out _);
        QueryEnvelope envelope = gate switch
        {
            "list" => ListTenantsEnvelope("admin", true),
            "detail" => GetTenantEnvelope("admin", true),
            "users" => GetTenantUsersEnvelope("admin", true),
            "memberships" => GetUserTenantsEnvelope("admin", TargetUser, true),
            "audit" => GetTenantAuditEnvelope("admin", true),
            _ => GetGlobalAdministratorsEnvelope("admin", true),
        };
        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, CreateCursorCodec(), envelope,
            administratorAuthority: authority);
        if (gate == "list") Deserialize<PaginatedResult<TenantSummary>>(result).Items.ShouldBeEmpty();
        else if (gate == "memberships") Deserialize<PaginatedResult<UserTenantMembership>>(result).Items.ShouldBeEmpty();
        else { result.Success.ShouldBeFalse(); result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden); }
    }

    [Theory]
    [InlineData("detail")]
    [InlineData("users")]
    public async Task NativeRevocationPreservesIndependentTenantOwnerPermission(string gate)
    {
        var store = Substitute.For<IReadModelStore>();
        SeedGlobalAdministrators(store, "admin");
        store.GetAsync<TenantReadModel>(TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantReadModel>(new TenantReadModel
            {
                TenantId = "tenant.alpha",
                Name = "Alpha",
                Status = TenantStatus.Active,
                Members = { ["admin"] = TenantRole.TenantOwner }
            }, "tenant-etag"));
        var authority = GlobalAdministratorSourceFixture.Authority(GlobalAdministratorSourceFixture.Source(revoked: true), out _, out _);
        var envelope = gate == "detail" ? GetTenantEnvelope("admin", true)
            : gate == "users" ? GetTenantUsersEnvelope("admin", true) : GetTenantAuditEnvelope("admin", true);
        (await TenantQueryTestHarness.ExecuteAsync(store, CreateCursorCodec(), envelope,
            administratorAuthority: authority)).Success.ShouldBeTrue();
    }
}
