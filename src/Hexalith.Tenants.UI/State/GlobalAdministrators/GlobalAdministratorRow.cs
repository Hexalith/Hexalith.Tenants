using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorRow(
    string UserId,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown) {
    /// <summary>
    /// Returns a support-safe description that omits the administrator identity.
    /// </summary>
    /// <remarks>
    /// The compiler-generated record ToString prints <c>UserId</c>, so any structured-logging destructure or
    /// interpolated message emitted a platform-authority identity. The list snapshot and request siblings were
    /// bounded for the same reason; the row they carry was not.
    /// </remarks>
    /// <returns>A bounded support-safe row description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorRow)} {{ Freshness = {Freshness}, Lifecycle = {Lifecycle} }}";

    public static GlobalAdministratorRow FromSummary(GlobalAdministratorSummary summary) {
        ArgumentNullException.ThrowIfNull(summary);

        return new(summary.UserId, ReadModelFreshnessState.Unknown);
    }
}
