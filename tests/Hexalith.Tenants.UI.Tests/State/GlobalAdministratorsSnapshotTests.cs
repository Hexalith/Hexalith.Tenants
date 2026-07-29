using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorsSnapshotTests
{
    [Fact]
    public void Ready_and_empty_factories_never_invent_complete_projection_evidence()
    {
        GlobalAdministratorsSnapshot ready = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current);
        GlobalAdministratorsSnapshot empty = GlobalAdministratorsSnapshot.Empty(
            isAuthorizationScoped: true,
            freshness: ReadModelFreshnessState.Current,
            eTag: "\"etag\"");

        ready.IsCompleteEvidence.ShouldBeFalse();
        empty.IsCompleteEvidence.ShouldBeFalse();
    }

    // Current freshness alone is not projection-confirmed evidence: a response carrying only the legacy
    // X-Hexalith-Is-Stale: false compatibility signal resolves to Current with no lifecycle evidence at all.
    // Mutation gating therefore requires both, matching ProjectionLifecyclePolicy.CanMutate.
    [Theory]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, true)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Unknown, false)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Stale, false)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Degraded, false)]
    [InlineData(ReadModelFreshnessState.Stale, ProjectionLifecycleState.Current, false)]
    [InlineData(ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Current, false)]
    [InlineData(ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, false)]
    public void Mutation_evidence_requires_current_freshness_and_current_projection_lifecycle(
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        bool expected)
    {
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: freshness) with
        {
            Lifecycle = lifecycle,
        };

        snapshot.IsMutationEvidenceBacked.ShouldBe(expected);
    }
}
