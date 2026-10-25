using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    [Fact]
    public void Pending_audit_probe_does_not_report_missing_audit_proof()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = RenderMemberFocusReview(Detail("tenant.alpha"), auditPending: true);

        cut.FindAll("[data-testid='tenants-remove-member-open']").ShouldBeEmpty();
        string reasons = string.Join('\n', cut.FindAll("[data-testid='tenants-member-reason-list']").Select(static list => list.TextContent));
        reasons.ShouldContain("audit proof pending");
        reasons.ShouldNotContain("missing audit proof");
    }

    [Fact]
    public void Resolved_missing_audit_capability_keeps_the_missing_proof_reason()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = RenderMemberFocusReview(
            Detail("tenant.alpha"),
            auditPending: false,
            auditAvailable: false);

        cut.FindAll("[data-testid='tenants-remove-member-open']").ShouldBeEmpty();
        string reasons = string.Join('\n', cut.FindAll("[data-testid='tenants-member-reason-list']").Select(static list => list.TextContent));
        reasons.ShouldContain("missing audit proof");
        reasons.ShouldNotContain("audit proof pending");
    }
}
