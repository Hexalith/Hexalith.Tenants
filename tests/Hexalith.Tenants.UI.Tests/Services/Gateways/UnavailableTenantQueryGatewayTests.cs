using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

// AC7: when neither query route is configured, the UI binds ITenantQueryGateway to
// UnavailableTenantQueryGateway and every read surface must fail closed.
// AC5/AC8: a fail-closed surface never fabricates "current" freshness and never echoes the
// caller-supplied ETag/cursor into any user-facing field.
public sealed class UnavailableTenantQueryGatewayTests
{
    private static UnavailableTenantQueryGateway CreateGateway() => new();

    [Fact]
    public async Task List_tenants_fails_closed_to_error_state_without_fabricating_freshness()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Cursor: "opaque-cursor", PageSize: 10, ETag: "\"secret-etag\""),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Error);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.NextCursor.ShouldBeNull();
        snapshot.HasMore.ShouldBeFalse();
        snapshot.ETag.ShouldBeNull();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        snapshot.ErrorMessage.ShouldNotContain("secret-etag", Case.Insensitive);
        snapshot.ErrorMessage.ShouldNotContain("opaque-cursor", Case.Insensitive);
    }

    [Fact]
    public async Task Get_tenant_fails_closed_to_unavailable_state_and_ignores_previous_snapshot()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();

        // A previously good snapshot must never be served as if current when the gateway is unconfigured.
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            new Hexalith.Tenants.Contracts.Queries.TenantDetail(
                "tenant.alpha",
                "Alpha",
                null,
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.UtcNow),
            eTag: "\"known\"",
            freshness: TenantFreshnessState.Current);

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha", ETag: "\"secret-etag\""),
            previous,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unavailable);
        snapshot.Detail.ShouldBeNull();
        snapshot.ETag.ShouldBeNull();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        snapshot.ErrorMessage.ShouldNotContain("secret-etag", Case.Insensitive);
    }

    [Fact]
    public async Task Get_my_tenants_fails_closed_to_unavailable_state()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();

        UserTenantMembershipSnapshot snapshot = await gateway.GetMyTenantsAsync(
            new UserTenantMembershipRequest(Cursor: "opaque-cursor"),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Unavailable);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.GatewayUnavailable);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Get_user_tenants_fails_closed_and_preserves_target_user()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();

        UserTenantMembershipSnapshot snapshot = await gateway.GetUserTenantsAsync(
            new UserTenantMembershipRequest(TargetUserId: "user-2", Cursor: "opaque-cursor"),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Unavailable);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.GatewayUnavailable);
        snapshot.TargetUserId.ShouldBe("user-2");
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
    }

    [Fact]
    public async Task Get_user_tenants_rejects_null_request()
        => await Should.ThrowAsync<ArgumentNullException>(() =>
            CreateGateway().GetUserTenantsAsync(null!, previous: null, CancellationToken.None));

    [Fact]
    public async Task Get_global_administrators_fails_closed_to_unavailable_state()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();

        GlobalAdministratorsSnapshot snapshot = await gateway.GetGlobalAdministratorsAsync(
            new GlobalAdministratorsRequest(Cursor: "opaque-cursor", ETag: "\"secret-etag\""),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Unavailable);
        snapshot.Reason.ShouldBe(GlobalAdministratorsReason.GatewayUnavailable);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Get_tenant_audit_fails_closed_and_preserves_request_scope()
    {
        UnavailableTenantQueryGateway gateway = CreateGateway();
        var request = new TenantAuditRequest(
            "tenant.alpha",
            From: DateTimeOffset.Parse("2026-06-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            To: DateTimeOffset.Parse("2026-06-07T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            Category: AuditEventCategory.Administrative,
            Cursor: "opaque-cursor");

        TenantAuditSnapshot snapshot = await gateway.GetTenantAuditAsync(request, previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Unavailable);
        snapshot.Reason.ShouldBe(TenantAuditReason.GatewayUnavailable);
        snapshot.TenantId.ShouldBe("tenant.alpha");
        snapshot.From.ShouldBe(request.From);
        snapshot.To.ShouldBe(request.To);
        snapshot.Category.ShouldBe(AuditEventCategory.Administrative.ToString());
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Get_tenant_audit_rejects_null_request()
        => await Should.ThrowAsync<ArgumentNullException>(() =>
            CreateGateway().GetTenantAuditAsync(null!, previous: null, CancellationToken.None));
}
