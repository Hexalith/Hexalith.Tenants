using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TruthState;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListRow(
    string TenantId,
    string Name,
    TenantStatus Status,
    TenantCountValue MemberCount,
    TenantCountValue OwnerCount,
    TenantPendingState PendingState,
    TenantFreshnessState Freshness) {
    public static TenantListRow FromSummary(TenantSummary summary) {
        ArgumentNullException.ThrowIfNull(summary);

        return new(
            summary.TenantId,
            summary.Name,
            summary.Status,
            TenantCountValue.Unknown,
            TenantCountValue.Unknown,
            TenantPendingState.None,
            TenantFreshnessState.Unknown);
    }
}
