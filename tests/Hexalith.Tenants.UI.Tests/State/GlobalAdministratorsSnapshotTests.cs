using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
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

    [Fact]
    public void Diagnostics_omit_rows_identities_cursors_validators_and_versions()
    {
        // Pins both new support-safe ToString overrides. Deleting either restores the compiler-generated
        // record ToString, which prints the string-typed NextCursor (the protected cursor), ETag,
        // ProjectionVersion and RequestCursor verbatim -- those three assertions are what carry this test.
        // The row-identity assertion is weaker than it looks: the generated ToString renders Rows as the
        // collection's type name, not its contents, so it would pass either way. It is kept as a guard
        // against a future override that does project row contents, not as evidence about today's.
        GlobalAdministratorsRequest request = new("cursor-secret", 20, "etag-secret");
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.alpha", ReadModelFreshnessState.Current)],
            nextCursor: "cursor-secret",
            hasMore: true,
            eTag: "etag-secret",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "version-secret",
            RequestCursor = "cursor-secret",
        };

        string diagnostic = $"{request} {snapshot}";

        diagnostic.ShouldNotContain("admin.alpha", Case.Sensitive);
        diagnostic.ShouldNotContain("cursor-secret", Case.Sensitive);
        diagnostic.ShouldNotContain("etag-secret", Case.Sensitive);
        diagnostic.ShouldNotContain("version-secret", Case.Sensitive);
    }

    [Fact]
    public void Snapshot_diagnostics_state_the_evidence_fields_a_reviewer_needs()
    {
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.alpha", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };

        snapshot.ToString().ShouldBe(
            "GlobalAdministratorsSnapshot { Kind = Ready, Freshness = Current, Reason = None, "
            + "Lifecycle = Current, IsCompleteEvidence = True, PagingRecovered = False }");
    }

    [Fact]
    public void Request_diagnostics_state_only_unprotected_shape()
    {
        GlobalAdministratorsRequest request = new("cursor-secret", 20, "etag-secret");

        request.ToString().ShouldBe("GlobalAdministratorsRequest { PageSize = 20 }");
    }

    /// <summary>
    /// The three records the diff bounded last are pinned exactly, like their snapshot and request siblings.
    /// Deleting any of the overrides restores the compiler-generated record ToString, which prints the
    /// administrator identity, the command MessageId and the CorrelationId into any structured-logging
    /// destructure or interpolated message. A substring-absence check would not catch a partial regression,
    /// so these assert the whole string.
    /// </summary>
    [Fact]
    public void Row_and_command_snapshots_keep_identities_and_correlation_out_of_their_descriptions()
    {
        GlobalAdministratorRow row = new("admin.secret", ReadModelFreshnessState.Current)
        {
            Lifecycle = ProjectionLifecycleState.Current,
        };
        GlobalAdministratorGrantCommandSnapshot grant = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(new SetGlobalAdministrator("admin.secret"));
        GlobalAdministratorRemoveCommandSnapshot remove = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(
                new RemoveGlobalAdministrator("admin.secret"),
                [
                    new GlobalAdministratorRow("admin.secret", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("admin.other", ReadModelFreshnessState.Current),
                ],
                isCompleteEvidence: true);

        row.ToString().ShouldBe("GlobalAdministratorRow { Freshness = Current, Lifecycle = Current }");
        grant.ToString().ShouldBe(
            "GlobalAdministratorGrantCommandSnapshot { State = RequestSent, HasIntent = True, "
            + "HasTrackedPreview = False, HasCommandEventEvidence = False, IsSubmissionAmbiguous = False, "
            + "AuditState = NotStarted, RejectionCode = , FocusTarget = Lifecycle, LiveRegionPoliteness = Polite }");
        remove.ToString().ShouldBe(
            "GlobalAdministratorRemoveCommandSnapshot { State = Previewed, HasIntent = True, "
            + "PreviewIsCompleteEvidence = True, AuditState = NotStarted, RejectionCode = , "
            + "FocusTarget = Lifecycle, LiveRegionPoliteness = Polite }");

        foreach (string description in new[] { row.ToString(), grant.ToString(), remove.ToString() })
        {
            description.ShouldNotContain("admin.secret");
            description.ShouldNotContain("admin.other");
        }
    }
}
