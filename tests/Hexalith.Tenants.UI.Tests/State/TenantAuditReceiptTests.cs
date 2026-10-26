using System.Globalization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAuditReceiptTests
{
    [Fact]
    public void Receipt_from_entry_derives_required_support_safe_fields()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(
            Entry(new Dictionary<string, string>
            {
                ["userId"] = "target-user",
                ["key"] = "billing.mode",
            }),
            ReadModelFreshnessState.Current);

        receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        receipt.Actor.ShouldBe("actor-user");
        receipt.Target.ShouldBe("target-user");
        receipt.Scope.ShouldBe("tenant.alpha");
        receipt.Outcome.ShouldBe("UserAddedToTenant");
        receipt.AuditReference.ShouldBe("event-safe-reference");
        receipt.CommandReference.ShouldBeNull();
        receipt.ProjectionMarker.ShouldBe(ReadModelFreshnessState.Current);
        receipt.TimestampLabel.ShouldBe("2026-06-01 10:00:00 UTC");
    }

    [Fact]
    public void Receipt_target_uses_typed_user_for_access_and_safe_fallbacks_for_administrative_evidence()
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

        TenantAuditEntry administrative = new(
            "event-administrative",
            "TenantConfigurationSet",
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            new Dictionary<string, string> { ["key"] = "billing.mode" });
        TenantAuditReceipt.FromEntry(administrative, ReadModelFreshnessState.Current)
            .Target.ShouldBe("billing.mode");
        TenantAuditReceipt.FromEntry(administrative with { NarrativePayload = new Dictionary<string, string>() }, ReadModelFreshnessState.Current)
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
        receipt.Actor.ShouldBe("actor-user");
        receipt.AuditReference.ShouldBe("event-safe-reference");
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
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, TenantAuditReceiptState.InvalidCursor)]
    [InlineData(TenantAuditSurfaceKind.Unavailable, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantAuditSurfaceKind.Error, TenantAuditReceiptState.Error)]
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
    public void Receipt_fields_omit_or_block_unsafe_values(string unsafeValue)
    {
        TenantAuditRow row = Row(
            eventReference: "event-safe-reference",
            actorId: unsafeValue,
            target: unsafeValue,
            referenceContext: $"userId: {unsafeValue}; key: billing.mode");

        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(row);

        receipt.Actor.ShouldBeEmpty();
        receipt.Target.ShouldBeEmpty();
        receipt.CommandReference.ShouldBeNull();
        receipt.AuditReference.ShouldBe("event-safe-reference");
        receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
    }

    [Fact]
    public void Receipt_with_missing_required_fields_is_partial_and_not_copyable()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row(eventReference: string.Empty));

        receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
    }

    [Fact]
    public void Unavailable_receipt_does_not_fabricate_timestamp_evidence()
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable();

        receipt.State.ShouldBe(TenantAuditReceiptState.InvalidReference);
        receipt.Actor.ShouldBeEmpty();
        receipt.Target.ShouldBeEmpty();
        receipt.Scope.ShouldBeEmpty();
        receipt.Outcome.ShouldBeEmpty();
        receipt.AuditReference.ShouldBeEmpty();
        receipt.CommandReference.ShouldBeNull();
        receipt.Timestamp.ShouldBeNull();
        receipt.TimestampLabel.ShouldBeEmpty();
    }

    [Fact]
    public void Ready_requires_current_projection_backed_absolute_evidence()
    {
        TenantAuditRow row = Row();
        TenantAuditReceipt.FromRow(row).State.ShouldBe(TenantAuditReceiptState.Ready);
        TenantAuditReceipt.FromRow(row with { Timestamp = default }).State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditReceipt.FromRow(row with { Timestamp = row.Timestamp.ToOffset(TimeSpan.FromHours(2)) }).State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditReceipt.FromRow(row with { Freshness = ReadModelFreshnessState.Unknown }).State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditReceipt.FromRow(row with { Lifecycle = ProjectionLifecycleState.Unknown }).State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditReceipt.FromRow(row with { Provenance = QueryResponseProvenance.Unknown }).State.ShouldBe(TenantAuditReceiptState.Partial);
    }

    [Theory]
    [InlineData("UnknownTenantEvent", AuditEventCategory.Access)]
    [InlineData("UserAddedToTenant", AuditEventCategory.Administrative)]
    [InlineData("TenantCreated", AuditEventCategory.Access)]
    public void Unknown_or_category_mismatched_outcome_cannot_become_available(
        string eventType,
        AuditEventCategory category)
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row() with
        {
            EventType = eventType,
            Category = category,
            Outcome = eventType,
        });

        receipt.Outcome.ShouldBeEmpty();
        receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantCommandAuditState auditState = TenantCommandAuditStates.FromConfirmationEvidence(receipt);
        auditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        TenantAuditAvailability.FromCommandAuditState(auditState).State
            .ShouldBe(TenantAuditAvailabilityState.MissingSupport);
    }

    [Fact]
    public void Access_receipt_keeps_target_fallbacks_visible_but_requires_a_safe_typed_user_for_ready_evidence()
    {
        TenantAuditRow configurationFallback = Row(target: "configuration.mode") with
        {
            ReferenceContext = "key: configuration.mode",
            Narrative = new TenantAuditNarrative(ConfigurationKey: "configuration.mode"),
        };
        TenantAuditRow tenantFallback = Row(target: "tenant.alpha") with
        {
            ReferenceContext = string.Empty,
            Narrative = new TenantAuditNarrative(),
        };

        TenantAuditReceipt configurationReceipt = TenantAuditReceipt.FromRow(configurationFallback);
        TenantAuditReceipt tenantReceipt = TenantAuditReceipt.FromRow(tenantFallback);

        configurationReceipt.Target.ShouldBe("configuration.mode");
        configurationReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        tenantReceipt.Target.ShouldBe("tenant.alpha");
        tenantReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Ready, true, TenantAuditReceiptState.InvalidReference)]
    [InlineData(TenantAuditSurfaceKind.Stale, true, TenantAuditReceiptState.Stale)]
    [InlineData(TenantAuditSurfaceKind.Degraded, true, TenantAuditReceiptState.Degraded)]
    [InlineData(TenantAuditSurfaceKind.Error, false, TenantAuditReceiptState.Error)]
    [InlineData(TenantAuditSurfaceKind.Unavailable, false, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantAuditSurfaceKind.Unauthorized, false, TenantAuditReceiptState.Unauthorized)]
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, false, TenantAuditReceiptState.InvalidCursor)]
    [InlineData(TenantAuditSurfaceKind.Loading, false, TenantAuditReceiptState.Loading)]
    public void Missing_requested_row_is_claimed_only_after_a_checked_page(
        TenantAuditSurfaceKind surface,
        bool expectedMissing,
        TenantAuditReceiptState expectedState)
    {
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable(surfaceKind: surface);
        receipt.State.ShouldBe(expectedState);
        receipt.IsRequestedReferenceMissing.ShouldBe(expectedMissing);
        receipt.Scope.ShouldBeEmpty();
        receipt.AuditReference.ShouldBeEmpty();
        receipt.Timestamp.ShouldBeNull();
    }

    [Fact]
    public void Receipt_model_contains_no_hidden_presentation_copy()
    {
        typeof(TenantAuditReceipt).GetProperty("CopyableReferenceText").ShouldBeNull();

        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string source = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "State",
            "TenantAudit",
            "TenantAuditReceipt.cs"));

        source.ShouldNotContain("StringBuilder");
        source.ShouldNotContain("AppendLine");
        source.ShouldNotContain("CopyableReferenceText");
    }

    [Theory]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Ready)]
    [InlineData(ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Partial)]
    [InlineData(ReadModelFreshnessState.Aging, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Partial)]
    [InlineData(ReadModelFreshnessState.Stale, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Stale)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Unknown, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Partial)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Rebuilding, QueryResponseProvenance.ProjectionBacked, TenantAuditReceiptState.Partial)]
    [InlineData(ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.Unknown, TenantAuditReceiptState.Partial)]
    public void ExplicitAuthorityEntryFactoryRequiresEveryAuthorityDimension(
        ReadModelFreshnessState freshness, ProjectionLifecycleState lifecycle,
        QueryResponseProvenance provenance, TenantAuditReceiptState expected)
    {
        TenantAuditEntry entry = Entry(new Dictionary<string, string> { ["userId"] = "target-user" });
        TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(entry, freshness, lifecycle, provenance);

        receipt.State.ShouldBe(expected);
        receipt.CommandReference.ShouldBeNull();
        TenantAuditRow row = TenantAuditRow.FromEntry(entry, freshness, lifecycle, provenance);
        row.Lifecycle.ShouldBe(lifecycle);
        row.Provenance.ShouldBe(provenance);
        TenantAuditReceipt.FromRow(row).ShouldBe(receipt);
        TenantAuditReceipt.FromEntry(entry, ReadModelFreshnessState.Current).State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditRow.FromEntry(entry, freshness).Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        TenantAuditRow.FromEntry(entry, freshness).Provenance.ShouldBe(QueryResponseProvenance.Unknown);
    }

    [Theory]
    [InlineData("GlobalAdministratorSet")]
    [InlineData("GlobalAdministratorRemoved")]
    public void ExplicitAuthorityRequiresAccessCategoryAndUserTargetForGlobalEvents(string eventType)
    {
        foreach (AuditEventCategory category in Enum.GetValues<AuditEventCategory>())
        {
            foreach (bool hasUser in new[] { false, true })
            {
                TenantAuditEntry entry = Entry(hasUser
                    ? new Dictionary<string, string> { ["userId"] = "target-user" }
                    : new Dictionary<string, string>()) with { EventType = eventType, Category = category };
                TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(entry, ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
                receipt.State.ShouldBe(category is AuditEventCategory.Access && hasUser
                    ? TenantAuditReceiptState.Ready : TenantAuditReceiptState.Partial);
                receipt.Outcome.ShouldBe(category is AuditEventCategory.Access ? eventType : string.Empty);
                receipt.CommandReference.ShouldBeNull();
            }
        }
    }

    [Theory]
    [InlineData("TenantConfigurationSet")]
    [InlineData("TenantConfigurationRemoved")]
    public void ConfigurationEvidenceRequiresASafeKeyAndTheMappedTypedTarget(string eventType)
    {
        TenantAuditEntry entry = Entry(new Dictionary<string, string> { ["key"] = "billing.mode" }) with
        {
            EventType = eventType,
            Category = AuditEventCategory.Administrative,
        };
        TenantAuditReceipt Receipt(TenantAuditEntry value) => TenantAuditReceipt.FromEntry(value,
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
        Receipt(entry).State.ShouldBe(TenantAuditReceiptState.Ready);
        Receipt(entry).Target.ShouldBe("billing.mode");

        foreach (Dictionary<string, string> payload in new Dictionary<string, string>[]
        {
            new(),
            new() { ["key"] = "Bearer raw-token" },
            new() { ["userId"] = "target-user" },
            new() { ["key"] = "Bearer raw-token", ["userId"] = "target-user" },
        })
        {
            TenantAuditReceipt receipt = Receipt(entry with { NarrativePayload = payload });
            receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
            receipt.Target.ShouldBe(payload.ContainsKey("userId") ? "target-user" : "tenant.alpha");
            receipt.CommandReference.ShouldBeNull();
        }

        TenantAuditRow row = TenantAuditRow.FromEntry(entry, ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
        foreach (string target in new[] { string.Empty, "Bearer raw-token", "other.key", "tenant.alpha" })
        {
            TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(row with { Target = target });
            receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
            receipt.Target.ShouldBe(target is "Bearer raw-token" ? string.Empty : target);
        }
        TenantAuditReceipt.FromRow(row with { Narrative = new TenantAuditNarrative(ConfigurationKey: "Bearer raw-token") })
            .State.ShouldBe(TenantAuditReceiptState.Partial);
        TenantAuditReceipt.FromRow(row with { Narrative = null }).State.ShouldBe(TenantAuditReceiptState.Partial);

        TenantAuditEntry bothFields = entry with
        {
            NarrativePayload = new Dictionary<string, string> { ["userId"] = "target-user", ["key"] = "billing.mode" },
        };
        Receipt(bothFields).State.ShouldBe(TenantAuditReceiptState.Ready);
        Receipt(bothFields).Target.ShouldBe("target-user");
        TenantAuditRow userTargetRow = TenantAuditRow.FromEntry(bothFields, ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
        foreach (string mismatchedTarget in new[] { "billing.mode", "other-user", "tenant.alpha" })
        {
            TenantAuditReceipt.FromRow(userTargetRow with { Target = mismatchedTarget }).State.ShouldBe(TenantAuditReceiptState.Partial);
        }
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Ready, TenantCommandAuditState.AuditPending, TenantAuditReceiptState.Pending)]
    [InlineData(TenantAuditSurfaceKind.Ready, TenantCommandAuditState.AuditDelayed, TenantAuditReceiptState.Delayed)]
    [InlineData(TenantAuditSurfaceKind.Ready, TenantCommandAuditState.AuditUnavailable, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantAuditSurfaceKind.Ready, TenantCommandAuditState.MissingSupport, TenantAuditReceiptState.MissingSupport)]
    [InlineData(TenantAuditSurfaceKind.Loading, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.Loading)]
    [InlineData(TenantAuditSurfaceKind.Error, TenantCommandAuditState.AuditPending, TenantAuditReceiptState.Error)]
    [InlineData(TenantAuditSurfaceKind.Unauthorized, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.Unauthorized)]
    [InlineData(TenantAuditSurfaceKind.Unavailable, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.Unavailable)]
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.InvalidCursor)]
    [InlineData(TenantAuditSurfaceKind.Stale, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.Stale)]
    [InlineData(TenantAuditSurfaceKind.Degraded, TenantCommandAuditState.AuditAvailable, TenantAuditReceiptState.Degraded)]
    public void ExplicitAuthorityDoesNotOverrideSurfaceOrCommandAuditState(
        TenantAuditSurfaceKind surface, TenantCommandAuditState audit, TenantAuditReceiptState expected)
    {
        TenantAuditReceipt.FromEntry(Entry(new Dictionary<string, string> { ["userId"] = "target-user" }),
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked,
            surface, audit).State.ShouldBe(expected);
    }

    [Fact]
    public void ExplicitAuthorityCannotPromoteUnsafeOrMissingEvidence()
    {
        TenantAuditEntry safe = Entry(new Dictionary<string, string> { ["userId"] = "target-user" });
        TenantAuditEntry[] incomplete =
        [
            safe with { ActorId = "Bearer raw-token" },
            safe with { TenantId = "Bearer raw-token" },
            safe with { NarrativePayload = new Dictionary<string, string> { ["userId"] = "person@example.test" } },
            safe with { EventType = "FutureTenantEvent" },
            safe with { Category = AuditEventCategory.Administrative },
            safe with { Timestamp = default },
            safe with { Timestamp = safe.Timestamp.ToOffset(TimeSpan.FromHours(2)) },
        ];
        foreach (TenantAuditEntry entry in incomplete)
        {
            TenantAuditReceipt receipt = TenantAuditReceipt.FromEntry(entry, ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
            receipt.State.ShouldBe(TenantAuditReceiptState.Partial);
            receipt.Actor.ShouldNotBe("Bearer raw-token");
            receipt.Target.ShouldNotBe("person@example.test");
            receipt.Scope.ShouldNotBe("Bearer raw-token");
            receipt.Outcome.ShouldNotBe("FutureTenantEvent");
        }

        Should.Throw<ArgumentException>(() => TenantAuditReceipt.FromEntry(safe with { EventId = "Bearer raw-token" },
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked));
        Should.Throw<ArgumentNullException>(() => TenantAuditReceipt.FromEntry(null!,
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void TimestampConventionIsInvariantAndNormalizesOffsetsWithoutInventingMissingTime(string cultureName)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row());
            receipt.TimestampLabel.ShouldBe("2026-06-01 10:00:00 UTC");
            (receipt with { Timestamp = receipt.Timestamp!.Value.ToOffset(TimeSpan.FromHours(-7)) })
                .TimestampLabel.ShouldBe("2026-06-01 10:00:00 UTC");
            (receipt with { Timestamp = null }).TimestampLabel.ShouldBeEmpty();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
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
            freshness,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked,
            new TenantAuditNarrative(UserId: target));
}
