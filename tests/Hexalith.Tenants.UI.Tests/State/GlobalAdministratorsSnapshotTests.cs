using Hexalith.EventStore.Client.Projections;
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
}
