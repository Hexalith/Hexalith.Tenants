using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorGrantCommandSnapshotTests
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    // The platform writes EventsStored/EventsPublished with no EventCount at all -- CommandStatusRecord
    // documents the field as "Completed status only" and AggregateActor's advisory writes leave it null --
    // so the null rows below are the shape the real gateway produces, not a lenient variant of it.
    [Theory]
    [InlineData(CommandStatus.EventsStored, null)]
    [InlineData(CommandStatus.EventsPublished, null)]
    [InlineData(CommandStatus.EventsStored, 1)]
    [InlineData(CommandStatus.EventsPublished, 1)]
    [InlineData(CommandStatus.Completed, 1)]
    public void EventProducingStatusWithVerifiedIdentityCanConfirmOnlyAdvancedCompleteProjection(
        CommandStatus status,
        int? eventCount)
    {
        GlobalAdministratorGrantCommandSnapshot pending = AcceptedAttempt()
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                EventCount: eventCount,
                HasVerifiedCommandIdentity: true));

        pending.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.HasCommandEventEvidence.ShouldBeTrue();

        GlobalAdministratorGrantCommandSnapshot confirmed = pending.ConfirmProjection(
            Complete("projection-v2", "existing-admin", "  User/CaseSensitive.01  "));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedProjection.ShouldNotBeNull().UserId.ShouldBe("  User/CaseSensitive.01  ");
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    // A completed command is the only status that reports how many events it produced, so it is the only
    // one whose count can prove the absence of an event.
    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public void CompletedStatusWithoutPositiveEventCountCannotConfirm(int? eventCount)
    {
        CommandStatus status = CommandStatus.Completed;
        GlobalAdministratorGrantCommandSnapshot result = AcceptedAttempt()
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                EventCount: eventCount,
                HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.HasCommandEventEvidence.ShouldBeFalse();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.UnableToVerify.EventEvidence");
        result.ConfirmProjection(Complete("projection-v2", "existing-admin", "  User/CaseSensitive.01  "))
            .State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(false, "Tenants.GlobalAdministrators.Grant.Status.Unknown")]
    [InlineData(true, "Tenants.GlobalAdministrators.Grant.Status.Pending")]
    public void MissingStatusIsClassifiedBeforeTrackingIdentity(bool pending, string expectedKey)
    {
        TenantCommandStatusResult status = pending
            ? TenantCommandStatusResult.Pending("Still propagating.")
            : TenantCommandStatusResult.Unknown("Not available.");

        GlobalAdministratorGrantCommandSnapshot result = AcceptedAttempt().ApplyStatus(status);

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe(expectedKey);
        result.SafeMessageKey.ShouldNotBe("Tenants.GlobalAdministrators.Grant.UnableToVerify.TrackingMismatch");
    }

    [Fact]
    public void NonNullStatusWithoutVerifiedFixedCommandIdentityFailsClosed()
    {
        GlobalAdministratorGrantCommandSnapshot result = AcceptedAttempt().ApplyStatus(
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.HasCommandEventEvidence.ShouldBeFalse();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.UnableToVerify.TrackingMismatch");
    }

    [Theory]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.Processing)]
    public void EarlierLifecycleStatusCannotRegressVerifiedEventEvidence(CommandStatus status)
    {
        GlobalAdministratorGrantCommandSnapshot projectionPending = AcceptedAttempt().ApplyStatus(
            new TenantCommandStatusResult(
                CommandStatus.EventsStored,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        GlobalAdministratorGrantCommandSnapshot result = projectionPending.ApplyStatus(
            new TenantCommandStatusResult(status, HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.HasCommandEventEvidence.ShouldBeTrue();
        result.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void SignalRNudgeRequestsRefreshWithoutInventingLifecycleOrAuditEvidence()
    {
        GlobalAdministratorGrantCommandSnapshot accepted = AcceptedAttempt();

        GlobalAdministratorGrantCommandSnapshot result = accepted.SignalRNudge();

        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.AuditState.ShouldBe(accepted.AuditState);
        result.HasCommandEventEvidence.ShouldBeFalse();
        result.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Fact]
    public void TargetPresenceAtUnchangedVersionDoesNotConfirm()
    {
        GlobalAdministratorGrantCommandSnapshot result = EventBackedAttempt().ConfirmProjection(
            Complete("projection-v1", "existing-admin", "  User/CaseSensitive.01  "));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.LastConfirmedProjection.ShouldBeNull();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.VersionNotAdvanced");
    }

    [Fact]
    public void AdvancedVersionWithoutTargetDoesNotConfirm()
    {
        GlobalAdministratorGrantCommandSnapshot result = EventBackedAttempt().ConfirmProjection(
            Complete("projection-v2", "existing-admin", "unrelated-admin"));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.LastConfirmedProjection.ShouldBeNull();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.DidNotConfirm");
    }

    [Fact]
    public void UnequalOpaqueVersionConfirmsWithoutNumericOrOrderedParsing()
    {
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "target-admin",
            Complete("opaque:zeta", "existing-admin"),
            isAuthorized: true);
        GlobalAdministratorGrantCommandSnapshot eventBacked = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .Preview(preview, MessageId)
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        GlobalAdministratorGrantCommandSnapshot result = eventBacked.ConfirmProjection(
            Complete("opaque:alpha", "existing-admin", "target-admin"));

        result.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        result.LastConfirmedProjection.ShouldNotBeNull().UserId.ShouldBe("target-admin");
        result.BaselineProjectionVersion.ShouldBe("opaque:zeta");
    }

    [Fact]
    public void PageScopedTargetPresenceDoesNotConfirm()
    {
        GlobalAdministratorsSnapshot page = Complete(
            "projection-v2",
            "existing-admin",
            "  User/CaseSensitive.01  ") with
        {
            IsCompleteEvidence = false,
            HasMore = true,
            NextCursor = "protected-next",
        };

        GlobalAdministratorGrantCommandSnapshot result = EventBackedAttempt().ConfirmProjection(page);

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.LastConfirmedProjection.ShouldBeNull();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.EvidenceRequired");
    }

    [Fact]
    public void VersionChangeWithoutExactCommandEventEvidenceDoesNotConfirm()
    {
        GlobalAdministratorGrantCommandSnapshot result = AcceptedAttempt().ConfirmProjection(
            Complete("projection-v2", "existing-admin", "  User/CaseSensitive.01  "));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.LastConfirmedProjection.ShouldBeNull();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.EvidenceRequired");
    }

    [Fact]
    public void MismatchedAcceptanceIdentityRetainsSameAttemptAsAmbiguous()
    {
        GlobalAdministratorGrantCommandSnapshot requestSent = PreviewedAttempt().RequestSent();

        GlobalAdministratorGrantCommandSnapshot result = requestSent.Accepted(
            TenantCommandSubmissionResult.Accepted("01BX5ZZKBKACTAV9WEVGEMMVRZ", "correlation-1"));

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsSubmissionAmbiguous.ShouldBeTrue();
        result.MessageId.ShouldBe(MessageId);
        result.PreviewEvidence.ShouldBeSameAs(requestSent.PreviewEvidence);
    }

    [Fact]
    public void BlankAcceptanceCorrelationRetainsSameAttemptAsAmbiguous()
    {
        GlobalAdministratorGrantCommandSnapshot requestSent = PreviewedAttempt().RequestSent();

        GlobalAdministratorGrantCommandSnapshot result = requestSent.Accepted(
            TenantCommandSubmissionResult.Accepted(MessageId, string.Empty));

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsSubmissionAmbiguous.ShouldBeTrue();
        result.MessageId.ShouldBe(MessageId);
        result.BaselineProjectionVersion.ShouldBe("projection-v1");
    }

    [Fact]
    public void MissingBffFactMakesPreviewIncomplete()
    {
        GlobalAdministratorGrantPreview preview = PreviewedAttempt().PreviewEvidence! with
        {
            KnownUnknownsFactKey = null,
        };

        preview.IsComplete.ShouldBeFalse();
        GlobalAdministratorGrantCommandSnapshot.Idle().Preview(preview, MessageId)
            .State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
    }

    [Fact]
    public void AlreadyAdministratorRejectionStaysRejected()
    {
        GlobalAdministratorGrantCommandSnapshot result = PreviewedAttempt()
            .RequestSent()
            .ApplySubmission(TenantCommandSubmissionResult.Rejected(
                "This user is already a global administrator.",
                "GlobalAdministratorAlreadyExists"));

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        result.RejectionCode.ShouldBe("GlobalAdministratorAlreadyExists");
    }

    private static GlobalAdministratorGrantCommandSnapshot EventBackedAttempt()
        => AcceptedAttempt().ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Completed,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));

    private static GlobalAdministratorGrantCommandSnapshot AcceptedAttempt()
        => PreviewedAttempt()
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"));

    private static GlobalAdministratorGrantCommandSnapshot PreviewedAttempt()
    {
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "  User/CaseSensitive.01  ",
            Complete("projection-v1", "existing-admin"),
            isAuthorized: true);
        return GlobalAdministratorGrantCommandSnapshot.Idle().Preview(preview, MessageId);
    }

    private static GlobalAdministratorsSnapshot Complete(string projectionVersion, params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(static userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: $"\"{projectionVersion}\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = projectionVersion,
            IsCompleteEvidence = true,
        };
}
