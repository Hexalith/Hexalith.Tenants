using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantDomIdentityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    [InlineData(" tenant.alpha ")]
    [InlineData("tenant[alpha]")]
    [InlineData("租户🙂")]
    public void Unsupported_routes_do_not_produce_detail_or_audit_navigation(string? tenantId)
    {
        TenantListNavigationContext navigation = new(State(tenantId, null));
        TenantListRow row = new(tenantId!, "display", TenantStatus.Active, TenantCountValue.Unknown,
            TenantCountValue.Unknown, TenantPendingState.None, ReadModelFreshnessState.Current);
        navigation.ToDetailUrl(row).ShouldBeEmpty();
        navigation.ToAuditUrl(row).ShouldBeEmpty();
        navigation.ToDetailUrl(tenantId, TenantDomIdentity.ElementId("tenant-row-", tenantId)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("all")]
    [InlineData("mine")]
    public void Selection_query_preserves_significant_identity_whitespace(string scope)
    {
        const string tenantId = " tenant.alpha ";
        string anchor = TenantDomIdentity.ElementId("tenant-row-", tenantId)!;
        TenantWorkspaceState state = State(tenantId, anchor, scope);
        state.SelectedTenantId.ShouldBe(tenantId);
        TenantAuditNavigationSafety.ReturnQueryValue(state.ToCanonicalUrl(), "selected").ShouldBe(tenantId);
    }

    [Fact]
    public void Encoded_focus_retains_existing_bounds_and_route_denials()
    {
        string allowed = new('a', 256);
        string focus = TenantDomIdentity.ElementId("tenant-row-", allowed)!;
        focus.Length.ShouldBeGreaterThan(256);
        TenantAuditNavigationSafety.IsSafeFocus(focus).ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeFocus(TenantDomIdentity.ElementId("tenant-row-", new string('a', 400))).ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus("tenants-member-" + focus["tenant-row-".Length..]).ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeTenantId("tenant alpha").ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeTenantId("租户🙂").ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeTenantId(new string('a', 257)).ShouldBeFalse();
    }

    private static TenantWorkspaceState State(string? selected, string? anchor, string? scope = null)
        => TenantWorkspaceState.FromQuery(null, scope, null, null, null, null, null, null, selected, anchor);

    [Fact]
    public void Encoded_focus_rejects_malformed_payloads_and_enforces_the_exact_existing_boundary()
    {
        string canonical = TenantDomIdentity.ElementId("tenant-row-", new string('a', 190))!;
        canonical["tenant-row-".Length..].Length.ShouldBeGreaterThan(256);
        TenantAuditNavigationSafety.IsSafeFocus(canonical).ShouldBeTrue();
        TenantAuditNavigationSafety.IsSafeFocus(canonical[..^1] + "R").ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus("tenant-row-id-" + new string('a', 257)).ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus(canonical + "=").ShouldBeFalse();
        string invalidUtf8 = Convert.ToBase64String(Enumerable.Repeat((byte)255, 190).ToArray()).TrimEnd('=').Replace('/', '_');
        TenantAuditNavigationSafety.IsSafeFocus("tenant-row-id-" + invalidUtf8).ShouldBeFalse();
        string blank = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(' ', 190))).TrimEnd('=');
        TenantAuditNavigationSafety.IsSafeFocus("tenant-row-id-" + blank).ShouldBeFalse();
        TenantAuditNavigationSafety.IsSafeFocus(TenantDomIdentity.ElementId("tenant-row-", new string('a', 257))).ShouldBeFalse();
        string boundary = TenantDomIdentity.ElementId("tenant-row-", new string('é', 117) + new string('a', 139))!;
        boundary.Length.ShouldBe(512);
        TenantAuditNavigationSafety.IsSafeFocus(boundary).ShouldBeTrue();
        string overBoundary = TenantDomIdentity.ElementId("tenant-row-", new string('é', 118) + new string('a', 138))!;
        overBoundary.Length.ShouldBe(513);
        TenantAuditNavigationSafety.IsSafeFocus(overBoundary).ShouldBeFalse();
    }
}
