using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorRow(
    string UserId,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown) {
    public static GlobalAdministratorRow FromSummary(GlobalAdministratorSummary summary) {
        ArgumentNullException.ThrowIfNull(summary);

        return new(summary.UserId, ReadModelFreshnessState.Unknown);
    }
}
