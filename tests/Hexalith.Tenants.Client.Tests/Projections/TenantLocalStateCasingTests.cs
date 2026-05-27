using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Projections;

/// <summary>
/// Locks in the Identifier Casing Contract (TEN-3): membership is keyed case-sensitively (Ordinal),
/// and a freshly built local state is never implicitly active (TEN-2). A future switch to
/// OrdinalIgnoreCase or a default of Active must fail these tests.
/// </summary>
public sealed class TenantLocalStateCasingTests {
    [Fact]
    public void Members_are_keyed_case_sensitively() {
        var state = new TenantLocalState { TenantId = "acme" };

        state.Members["User-1"] = TenantRole.TenantOwner;
        state.Members["user-1"] = TenantRole.TenantReader;

        state.Members.Count.ShouldBe(2);
        state.Members.ContainsKey("USER-1").ShouldBeFalse();
    }

    [Fact]
    public void Clone_preserves_case_sensitive_membership() {
        var state = new TenantLocalState { TenantId = "acme" };
        state.Members["User-1"] = TenantRole.TenantOwner;
        state.Members["user-1"] = TenantRole.TenantReader;

        TenantLocalState clone = state.Clone();

        clone.Members.Count.ShouldBe(2);
        clone.Members["User-1"].ShouldBe(TenantRole.TenantOwner);
        clone.Members["user-1"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public void New_local_state_status_defaults_to_Unknown() {
        new TenantLocalState().Status.ShouldBe(TenantStatus.Unknown);
    }
}
