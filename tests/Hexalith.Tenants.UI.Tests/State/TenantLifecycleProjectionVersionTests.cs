using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantLifecycleProjectionVersionTests
{
    [Theory]
    [InlineData("tenant-sequence:42", "tenant-sequence:41", "IncomingNewer")]
    [InlineData("tenant-sequence:41", "tenant-sequence:42", "IncomingOlder")]
    [InlineData("tenant-sequence:41", "tenant-sequence:41", "Equal")]
    public void Comparable_tenant_sequences_return_a_distinct_ordered_relation(
        string incoming,
        string retained,
        string expected)
        => TenantLifecycleProjectionVersion.CompareSequences(incoming, retained).ToString().ShouldBe(expected);

    [Theory]
    [InlineData(null, "tenant-sequence:41")]
    [InlineData("tenant-sequence:41", null)]
    [InlineData("opaque-etag-9", "opaque-etag-8")]
    [InlineData("other-sequence:42", "tenant-sequence:41")]
    [InlineData("tenant-sequence:41", "other-sequence:41")]
    public void Incomparable_markers_are_not_treated_as_equal_or_ordered(string? incoming, string? retained)
        => TenantLifecycleProjectionVersion.CompareSequences(incoming, retained)
            .ToString()
            .ShouldBe(nameof(TenantLifecycleSequenceRelation.Incomparable));

    [Theory]
    [InlineData(null, "tenant-sequence:41", "Invalid")]
    [InlineData("tenant-sequence:41", null, "Invalid")]
    [InlineData("opaque", "opaque-2", "Invalid")]
    [InlineData("etag-v1", "etag-v2", "PrefixMismatch")]
    [InlineData("tenant-sequence:41", "other-sequence:42", "PrefixMismatch")]
    [InlineData("tenant-sequence:41", "tenant-sequence:41", "NotAdvanced")]
    [InlineData("tenant-sequence:42", "tenant-sequence:41", "NotAdvanced")]
    [InlineData("tenant-sequence:41", "tenant-sequence:42", "Advanced")]
    public void Compare_classifies_ordered_advancement_without_accepting_incomparable_markers(
        string? baseline,
        string? current,
        string expected)
        => TenantLifecycleProjectionVersion.Compare(baseline, current).ToString().ShouldBe(expected);

    [Fact]
    public void Incomparable_confirmation_proof_does_not_replace_a_tenant_sequence_last_observed_version()
    {
        TenantLifecycleCommandSnapshot pending = Pending();

        TenantLifecycleCommandSnapshot observed = pending.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "other-sequence:99"));
        TenantLifecycleCommandSnapshot olderTenantSequence = observed.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "tenant-sequence:40"));

        pending.LastObservedProjectionVersion.ShouldBe("tenant-sequence:41");
        observed.LastObservedProjectionVersion.ShouldBe("tenant-sequence:41");
        olderTenantSequence.LastObservedProjectionVersion.ShouldBe("tenant-sequence:41");
        ReferenceEquals(olderTenantSequence, observed).ShouldBeTrue();
    }

    private static TenantLifecycleCommandSnapshot Pending()
    {
        TenantDetail detail = new(
            "tenant.alpha",
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        return TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(intent, detail, "tenant-sequence:41")
            .RequestSent(intent, detail, "tenant-sequence:41", "message-1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));
    }

    private static TenantDetailSnapshot Proof(string tenantId, TenantStatus status, string projectionVersion)
        => TenantDetailSnapshot.Ready(
            new TenantDetail(
                tenantId,
                "Alpha",
                "Tenant alpha description",
                status,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
            eTag: null,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            projectionVersion);
}
