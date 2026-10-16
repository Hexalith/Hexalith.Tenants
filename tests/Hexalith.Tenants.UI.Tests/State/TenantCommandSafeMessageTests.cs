using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Protects fresh localized failure messages without retaining stale transition copy.</summary>
public sealed class TenantCommandSafeMessageTests
{
    [Theory]
    [InlineData(CommandStatus.Rejected)]
    [InlineData(CommandStatus.PublishFailed)]
    [InlineData(CommandStatus.TimedOut)]
    public void FailureTransitionsCarryTheFreshGatewayResourceKey(CommandStatus commandStatus)
    {
        const string key = "Tenants.Commands.Failure.ConcurrencyConflict";
        TenantCommandStatusResult status = new(commandStatus,
            SafeMessage: "Safe fallback", HasVerifiedCommandIdentity: true, SafeMessageKey: key);
        TenantCommandLifecycleState expected = commandStatus switch
        {
            CommandStatus.Rejected => TenantCommandLifecycleState.Rejected,
            CommandStatus.PublishFailed => TenantCommandLifecycleState.Degraded,
            _ => TenantCommandLifecycleState.UnableToVerify,
        };
        TenantCreateCommandSnapshot create = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");
        TenantAddMemberCommandSnapshot add = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");
        TenantChangeRoleCommandSnapshot change = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");
        TenantRemoveMemberCommandSnapshot remove = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");
        TenantUpdateMetadataCommandSnapshot update = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");
        TenantLifecycleCommandSnapshot lifecycle = new(TenantCommandLifecycleState.Accepted, MessageId: "original", SafeMessageKey: "stale");

        create.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        add.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        change.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        remove.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        update.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        lifecycle.ApplyStatus(status).SafeMessageKey.ShouldBe(key);
        create.ApplyStatus(status).State.ShouldBe(expected);
        add.ApplyStatus(status).State.ShouldBe(expected);
        change.ApplyStatus(status).State.ShouldBe(expected);
        remove.ApplyStatus(status).State.ShouldBe(expected);
        update.ApplyStatus(status).State.ShouldBe(expected);
        lifecycle.ApplyStatus(status).State.ShouldBe(expected);
        create.ApplyStatus(status).MessageId.ShouldBe("original");
        remove.ApplyStatus(status).MessageId.ShouldBe("original");
        lifecycle.ApplyStatus(status).MessageId.ShouldBe("original");
    }

    [Theory]
    [InlineData(CommandStatus.Processing)]
    [InlineData(CommandStatus.EventsStored)]
    [InlineData(CommandStatus.Completed)]
    public void ProgressTransitionsClearPreviousFailureCopy(CommandStatus commandStatus)
    {
        TenantCommandStatusResult status = new(commandStatus, EventCount: 1,
            SafeMessage: "Previous failure copy", SafeMessageKey: "Tenants.Commands.Failure.ConcurrencyConflict",
            HasVerifiedCommandIdentity: true);
        TenantCreateCommandSnapshot create = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale");
        TenantAddMemberCommandSnapshot add = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale");
        TenantChangeRoleCommandSnapshot change = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale");
        TenantRemoveMemberCommandSnapshot remove = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale");
        TenantUpdateMetadataCommandSnapshot update = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale");
        TenantLifecycleCommandSnapshot lifecycle = new(TenantCommandLifecycleState.Accepted, SafeMessageKey: "stale",
            AttemptStartedAtUtc: DateTimeOffset.UtcNow);
        create.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        add.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        change.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        remove.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        update.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        lifecycle.ApplyStatus(status).SafeMessageKey.ShouldBeNull();
        lifecycle.ApplyStatus(status).SafeMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownEnumRejectsIncomingTextAndUsesTheFreshKeyOrLocalizedFallback(bool hasCurrentKey)
    {
        const string freshKey = "Tenants.Commands.Status.ResponseUnavailable";
        TenantCommandStatusResult status = new((CommandStatus)999,
            SafeMessage: "backend-secret-Canary",
            SafeMessageKey: hasCurrentKey ? freshKey : null, HasVerifiedCommandIdentity: true);
        foreach (var snapshot in SharedSnapshotResults(status))
        {
            snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
            snapshot.Key.ShouldBe(hasCurrentKey ? freshKey : "Tenants.Commands.Status.Unverified");
            snapshot.Message.ShouldBe("Command status could not be verified.");
            snapshot.Message.ShouldNotBeNull().ShouldNotContain("backend-secret-Canary");
            snapshot.MessageId.ShouldBe("original-message");
            snapshot.CorrelationId.ShouldBe("original-correlation");
            snapshot.Audit.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        }
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("retryable")]
    [InlineData("unknown")]
    public void KeyedLookupFactoriesPreserveFlagsAndReplaceStaleSnapshotCopy(string kind)
    {
        const string key = "Tenants.Commands.Status.ResponseUnavailable";
        TenantCommandStatusResult status = kind switch
        {
            "pending" => TenantCommandStatusResult.PendingWithKey(key),
            "retryable" => TenantCommandStatusResult.RetryableFailureWithKey(key),
            _ => TenantCommandStatusResult.UnknownWithKey(key),
        };
        status.Status.ShouldBeNull();
        status.SafeMessage.ShouldBeNull();
        status.IsPending.ShouldBe(kind == "pending");
        status.IsRetryableFailure.ShouldBe(kind == "retryable");
        status.HasVerifiedCommandIdentity.ShouldBeFalse();
        status.CommittedEventSequence.ShouldBeNull();
        status.EventCount.ShouldBeNull();
        status.RejectionCode.ShouldBeNull();
        foreach (var snapshot in SharedSnapshotResults(status))
        {
            snapshot.Key.ShouldBe(key);
            snapshot.Message.ShouldBeNull();
            snapshot.State.ShouldBe(kind == "unknown" ? TenantCommandLifecycleState.UnableToVerify : TenantCommandLifecycleState.Accepted);
            snapshot.MessageId.ShouldBe("original-message");
            snapshot.CorrelationId.ShouldBe("original-correlation");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StrongerMembershipExpiryCopySurvivesTemporaryLookupMessages(bool retryable)
    {
        string expiredKey = TenantMembershipProjectionWaitPolicy.ExpiredMessageKey;
        TenantCommandStatusResult temporary = retryable
            ? TenantCommandStatusResult.RetryableFailureWithKey("Tenants.Commands.Status.RetryableFailure")
            : TenantCommandStatusResult.PendingWithKey("Tenants.Commands.Status.Pending");
        TenantAddMemberCommandSnapshot add = new(TenantCommandLifecycleState.UnableToVerify,
            MessageId: "original", SafeMessageKey: expiredKey, HasCommandEventEvidence: true)
        { ProjectionWaitExpired = true };
        TenantChangeRoleCommandSnapshot change = new(TenantCommandLifecycleState.UnableToVerify,
            MessageId: "original", SafeMessageKey: expiredKey, HasCommandEventEvidence: true)
        { ProjectionWaitExpired = true };
        TenantRemoveMemberCommandSnapshot remove = new(TenantCommandLifecycleState.UnableToVerify,
            MessageId: "original", SafeMessageKey: expiredKey, HasCommandEventEvidence: true)
        { ProjectionWaitExpired = true, CommittedEventSequence = 17 };
        add.ApplyStatus(temporary).SafeMessageKey.ShouldBe(expiredKey);
        change.ApplyStatus(temporary).SafeMessageKey.ShouldBe(expiredKey);
        remove.ApplyStatus(temporary).SafeMessageKey.ShouldBe(expiredKey);
        add.ApplyStatus(temporary).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        change.ApplyStatus(temporary).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        remove.ApplyStatus(temporary).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        add.ApplyStatus(temporary).HasCommandEventEvidence.ShouldBeTrue();
        change.ApplyStatus(temporary).HasCommandEventEvidence.ShouldBeTrue();
        remove.ApplyStatus(temporary).HasCommandEventEvidence.ShouldBeTrue();
        remove.ApplyStatus(temporary).CommittedEventSequence.ShouldBe(17);
        remove.ApplyStatus(temporary).MessageId.ShouldBe("original");
    }

    [Fact]
    public void ZeroEventRoleCompletionReplacesStaleFailureWithLocalizedAlreadyAppliedCopy()
    {
        TenantChangeRoleCommandSnapshot change = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original", CorrelationId: "correlation", SafeMessage: "Old failure",
            SafeMessageKey: "Tenants.Commands.Failure.ConcurrencyConflict");
        TenantChangeRoleCommandSnapshot result = change.ApplyStatus(new(CommandStatus.Completed,
            SafeMessage: "Earlier status detail", SafeMessageKey: "Tenants.Commands.Status.ResponseUnavailable",
            EventCount: 0, HasVerifiedCommandIdentity: true));
        result.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        result.SafeMessageKey.ShouldBe("Tenants.ChangeRole.Status.AlreadyApplied");
        result.HasCommandEventEvidence.ShouldBeFalse();
        result.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        result.MessageId.ShouldBe("original");
        result.CorrelationId.ShouldBe("correlation");
    }

    [Fact]
    public void ConfigurationConsumersKeepTheirIntentionalOperationSpecificStatusMessages()
    {
        TenantCommandStatusResult status = new(CommandStatus.Rejected,
            SafeMessageKey: "Tenants.Commands.Rejection.TenantDisabled", RejectionCode: "TenantDisabled",
            HasVerifiedCommandIdentity: true);
        TenantSetConfigurationCommandSnapshot set = new(TenantCommandLifecycleState.Accepted);
        TenantRemoveConfigurationCommandSnapshot remove = new(TenantCommandLifecycleState.Accepted);
        set.ApplyStatus(status).SafeMessageKey.ShouldBe("Tenants.Configuration.Set.Status.Rejected");
        remove.ApplyStatus(status).SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.Status.Rejected");
    }

    private static IEnumerable<(TenantCommandLifecycleState State, string? Message, string? Key,
        string? MessageId, string? CorrelationId, TenantCommandAuditState Audit)> SharedSnapshotResults(TenantCommandStatusResult status)
    {
        TenantCreateCommandSnapshot create = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original-message", CorrelationId: "original-correlation", SafeMessage: "Old failure", SafeMessageKey: "stale");
        TenantAddMemberCommandSnapshot add = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original-message", CorrelationId: "original-correlation", SafeMessage: "Old failure", SafeMessageKey: "stale");
        TenantChangeRoleCommandSnapshot change = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original-message", CorrelationId: "original-correlation", SafeMessage: "Old failure", SafeMessageKey: "stale");
        TenantRemoveMemberCommandSnapshot remove = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original-message", CorrelationId: "original-correlation", SafeMessage: "Old failure", SafeMessageKey: "stale");
        TenantUpdateMetadataCommandSnapshot metadata = new(TenantCommandLifecycleState.Accepted,
            MessageId: "original-message", CorrelationId: "original-correlation", SafeMessage: "Old failure", SafeMessageKey: "stale");
        create = create.ApplyStatus(status);
        add = add.ApplyStatus(status);
        change = change.ApplyStatus(status);
        remove = remove.ApplyStatus(status);
        metadata = metadata.ApplyStatus(status);
        yield return (create.State, create.SafeMessage, create.SafeMessageKey, create.MessageId, create.CorrelationId, create.AuditState);
        yield return (add.State, add.SafeMessage, add.SafeMessageKey, add.MessageId, add.CorrelationId, add.AuditState);
        yield return (change.State, change.SafeMessage, change.SafeMessageKey, change.MessageId, change.CorrelationId, change.AuditState);
        yield return (remove.State, remove.SafeMessage, remove.SafeMessageKey, remove.MessageId, remove.CorrelationId, remove.AuditState);
        yield return (metadata.State, metadata.SafeMessage, metadata.SafeMessageKey, metadata.MessageId, metadata.CorrelationId, metadata.AuditState);
    }
}
