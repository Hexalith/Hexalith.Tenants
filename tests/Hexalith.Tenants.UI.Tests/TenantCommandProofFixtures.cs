using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>Builds explicitly qualified authoritative command proof for positive test fixtures.</summary>
internal static class TenantCommandProofFixtures
{
    internal static TenantDetailSnapshot? Proof(TenantDetail? detail, string? version = "tenant-sequence:2")
        => detail is null ? null : TenantDetailSnapshot.Ready(detail, "etag-proof",
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, version).WithCallerScope("test-caller");

    internal static TenantListSnapshot? ListProof(TenantSummary? summary, string? version = null)
        => summary is null ? null : TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(summary) with { Freshness = ReadModelFreshnessState.Current,
                Lifecycle = ProjectionLifecycleState.Current }], null, false, "etag-list-proof",
            ReadModelFreshnessState.Current, false) with
            { Lifecycle = ProjectionLifecycleState.Current, ProjectionVersion = version, CallerScope = "test-caller" };
}
