using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListRow(
    string TenantId,
    string Name,
    TenantStatus Status,
    TenantCountValue MemberCount,
    TenantCountValue OwnerCount,
    TenantPendingState PendingState,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown) {
    public static TenantListRow FromSummary(TenantSummary summary) {
        ArgumentNullException.ThrowIfNull(summary);

        return new(
            summary.TenantId,
            summary.Name,
            summary.Status,
            TenantCountValue.Unknown,
            TenantCountValue.Unknown,
            TenantPendingState.None,
            ReadModelFreshnessState.Unknown);
    }
}
