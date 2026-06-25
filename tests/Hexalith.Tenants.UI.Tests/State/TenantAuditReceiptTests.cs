using System.Globalization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAuditReceiptTests
{
    [Fact]
    public void Receipt_from_entry_derives_required_fields_and_safe_copy_summary()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(
            Entry(new Dictionary<string, string>
            {
                ["userId"] = "target-user",
                ["key"] = "billing.mode",
            }),
            ReadModelFreshnessState.Current,
            supportSafeCommandReference: "command-safe-reference");

        receipt.State.ShouldBe(TenantAuditReceiptState.Ready);
        receipt.Actor.ShouldBe("actor-user");
        receipt.Target.ShouldBe("target-user");
        receipt.Scope.ShouldBe("tenant.alpha");
        receipt.Outcome.ShouldBe("UserAddedToTenant (Access)");
        receipt.AuditReference.ShouldBe("event-safe-reference");
        receipt.CommandReference.ShouldBe("command-safe-reference");
        receipt.ProjectionMarker.ShouldBe(ReadModelFreshnessState.Current);
        receipt.CopyableReferenceText.ShouldContain("event-safe-reference");
        receipt.CopyableReferenceText.ShouldContain("command-safe-reference");
        receipt.CopyableReferenceText.ShouldContain("tenant.alpha");
        receipt.CopyableReferenceText.ShouldContain("target-user");
        receipt.CopyableReferenceText.ShouldContain("2026-06-01 10:00:00 UTC");
    }

    [Fact]
    public void Receipt_target_fallback_uses_user_id_then_key_then_tenant()
    {
        TenantAuditReceipt.FromEntry(Entry(new Dictionary<string, string>
        {
            ["userId"] = "target-user",
            ["key"] = "billing.mode",
        }), ReadModelFreshnessState.Current).Target.ShouldBe("target-user");

        TenantAuditReceipt.FromEntry(Entry(new Dictionary<string, string>
        {
            ["key"] = "billing.mode",
        }), ReadModelFreshnessState.Current).Target.ShouldBe("billing.mode");

        TenantAuditReceipt.FromEntry(Entry(new Dictionary<string, string>()), ReadModelFreshnessState.Current)
            .Target.ShouldBe("tenant.alpha");
    }

    [Fact]
    public void Receipt_from_entry_treats_narrative_payload_as_structured_metadata_not_raw_body()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(
            Entry(new Dictionary<string, string>
            {
                ["userId"] = "target-user",
                ["key"] = "billing.mode",
                ["rawPayload"] = "raw payload token secret",
                ["authorization"] = "Bearer raw-token",
                ["correlationId"] = "internal-correlation-123",
                ["stackTrace"] = "System.InvalidOperationException stack trace",
            }),
            ReadModelFreshnessState.Current);

        receipt.Target.ShouldBe("target-user");
        receipt.CopyableReferenceText.ShouldContain("target-user");
        receipt.CopyableReferenceText.ShouldNotContain("raw payload", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("token", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("authorization", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("correlation", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("stack trace", Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, TenantAuditReceiptState.Pending)]
    [InlineData(TenantCommandAuditState.AuditDelayed, TenantAuditReceiptState.Delayed)]
    [InlineData(TenantCommandAuditState.AuditUnavailable, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantCommandAuditState.MissingSupport, TenantAuditReceiptState.MissingSupport)]
    public void Receipt_maps_command_audit_states_without_success(TenantCommandAuditState auditState, TenantAuditReceiptState expected)
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row(), auditState: auditState);

        receipt.State.ShouldBe(expected);
        receipt.State.ShouldNotBe(TenantAuditReceiptState.Ready);
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Stale, TenantAuditReceiptState.Stale)]
    [InlineData(TenantAuditSurfaceKind.Degraded, TenantAuditReceiptState.Degraded)]
    [InlineData(TenantAuditSurfaceKind.Unauthorized, TenantAuditReceiptState.Unauthorized)]
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, TenantAuditReceiptState.InvalidReference)]
    [InlineData(TenantAuditSurfaceKind.Unavailable, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantAuditSurfaceKind.Error, TenantAuditReceiptState.Unavailable)]
    public void Receipt_maps_audit_surface_states_without_false_success(TenantAuditSurfaceKind surfaceKind, TenantAuditReceiptState expected)
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row(), surfaceKind: surfaceKind);

        receipt.State.ShouldBe(expected);
        receipt.State.ShouldNotBe(TenantAuditReceiptState.Ready);
    }

    [Theory]
    [InlineData("Bearer raw-token")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.raw.payload")]
    [InlineData("EventStore metadata raw cursor etag")]
    [InlineData("System.InvalidOperationException stack trace")]
    [InlineData("internal-correlation-123")]
    [InlineData("MessageId 01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("person@example.test")]
    public void Receipt_copy_summary_omits_or_blocks_unsafe_values(string unsafeValue)
    {
        TenantAuditRow row = Row(
            eventReference: "event-safe-reference",
            actorId: unsafeValue,
            target: unsafeValue,
            referenceContext: $"userId: {unsafeValue}; key: billing.mode");

        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(row, supportSafeCommandReference: unsafeValue);

        receipt.CopyableReferenceText.ShouldNotContain(unsafeValue, Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("raw-token", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("payload", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("metadata", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("correlation", Case.Insensitive);
        receipt.CopyableReferenceText.ShouldNotContain("MessageId", Case.Insensitive);
    }

    [Fact]
    public void Receipt_with_missing_required_fields_is_partial_and_not_copyable()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row(eventReference: string.Empty));

        receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        receipt.CopyableReferenceText.ShouldBeEmpty();
    }

    [Fact]
    public void Unavailable_receipt_does_not_fabricate_timestamp_evidence()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable("requested-reference", "tenant.alpha");

        receipt.State.ShouldBe(TenantAuditReceiptState.InvalidReference);
        receipt.Timestamp.ShouldBeNull();
        receipt.TimestampLabel.ShouldBeEmpty();
        receipt.CopyableReferenceText.ShouldBeEmpty();
    }

    private static TenantAuditEntry Entry(IReadOnlyDictionary<string, string> narrative)
        => new(
            "event-safe-reference",
            "UserAddedToTenant",
            AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            narrative);

    private static TenantAuditRow Row(
        string eventReference = "event-safe-reference",
        string actorId = "actor-user",
        string target = "target-user",
        string referenceContext = "userId: target-user",
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current)
        => new(
            eventReference,
            "UserAddedToTenant",
            AuditEventCategory.Access,
            actorId,
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            target,
            "tenant.alpha",
            "UserAddedToTenant",
            referenceContext,
            freshness);
}
