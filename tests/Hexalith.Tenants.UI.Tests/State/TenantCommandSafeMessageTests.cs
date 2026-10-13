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
}
