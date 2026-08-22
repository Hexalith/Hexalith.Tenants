using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantLifecycleAttemptTrackerTests
{
    [Fact]
    public void Retained_attempt_preserves_literal_intent_stable_handle_baseline_and_event_evidence()
    {
        TenantLifecycleAttemptTracker tracker = new();
        TenantLifecycleCommandSnapshot pending = Pending("Tenant.Mixed-01", "message-1");

        tracker.Remember(pending);

        TenantLifecycleCommandSnapshot retained = tracker.Find("Tenant.Mixed-01").ShouldNotBeNull();
        retained.Intent.ShouldBe(pending.Intent);
        retained.MessageId.ShouldBe("message-1");
        retained.CorrelationId.ShouldBe("correlation-1");
        retained.BaselineProjectionVersion.ShouldBe("tenant-sequence:41");
        retained.HasCommandEventEvidence.ShouldBeTrue();
        tracker.Find("tenant.mixed-01").ShouldBeNull();
    }

    [Fact]
    public void Late_terminal_attempt_cannot_remove_a_newer_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = new();
        tracker.Remember(Pending("tenant.alpha", "message-new"));

        tracker.Forget("tenant.alpha", "message-old");

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
        tracker.Forget("tenant.alpha", "message-new");
        tracker.Find("tenant.alpha").ShouldBeNull();
    }

    private static TenantLifecycleCommandSnapshot Pending(string tenantId, string messageId)
    {
        TenantDetail detail = new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var intent = new TenantLifecycleCommandRequest(tenantId, TenantLifecycleOperation.DisableTenant);
        return TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(intent, detail)
            .RequestSent(intent, detail, "tenant-sequence:41", messageId)
            .Accepted(TenantCommandSubmissionResult.Accepted(messageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.EventsStored));
    }
}
