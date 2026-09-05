using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>
/// Pins the projection-version contract membership confirmation depends on.
/// </summary>
/// <remarks>
/// Confirmation requires an ordered advancement of an authoritative <c>ProjectionVersion</c>. The tenant
/// projection persists the aggregate-local EventStore sequence as <c>tenant-sequence:&lt;n&gt;</c>, while
/// handler-computed query responses publish no projection version and never substitute the state-store
/// ETag. Defensive comparison of legacy tokens remains fail-closed for hashes, GUIDs, and other opaque values.
/// </remarks>
public sealed class TenantMembershipCommandProvenanceTests
{
    [Theory]
    [InlineData("1", "2")]
    [InlineData("41", "42")]
    [InlineData("tenant-etag-1", "tenant-etag-2")]
    [InlineData("projection-v9", "projection-v10")]
    [InlineData("projection-v0009", "projection-v0010")]
    public void Ordered_state_store_versions_are_a_qualified_advancement(string baseline, string current)
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeTrue();

    [Theory]
    [InlineData("2", "1")]
    [InlineData("tenant-etag-2", "tenant-etag-1")]
    [InlineData("2", "2")]
    public void Regressed_or_unchanged_versions_are_not_an_advancement(string baseline, string current)
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeFalse();

    [Theory]
    [InlineData("opaque-etag", TenantProjectionVersionFormat.SequencePrefix + "42")]
    [InlineData("\"a3f9c2\"", TenantProjectionVersionFormat.SequencePrefix + "1")]
    [InlineData("legacy-etag-41", TenantProjectionVersionFormat.SequencePrefix + "42")]
    public void Exact_command_event_evidence_allows_one_way_migration_to_tenant_sequence(
        string baseline,
        string current)
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeTrue();

    [Theory]
    [InlineData(TenantProjectionVersionFormat.SequencePrefix + "42", TenantProjectionVersionFormat.SequencePrefix + "42")]
    [InlineData(TenantProjectionVersionFormat.SequencePrefix + "42", TenantProjectionVersionFormat.SequencePrefix + "41")]
    [InlineData(TenantProjectionVersionFormat.SequencePrefix + "42", "legacy-etag-43")]
    [InlineData(TenantProjectionVersionFormat.SequencePrefix + "not-a-number", TenantProjectionVersionFormat.SequencePrefix + "43")]
    [InlineData("legacy-etag-42", TenantProjectionVersionFormat.SequencePrefix + "not-a-number")]
    public void Tenant_sequence_regression_reverse_migration_and_malformed_tokens_fail_closed(
        string baseline,
        string current)
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeFalse();

    [Theory]
    [InlineData("a3f9c2", "b7e1d4")]
    [InlineData("9f1c8b2e-4a6d-4f27-9c31-7b0e5a2d8c14", "0d2e7a91-3c58-4b16-8e40-1f6b9c3a7d52")]
    [InlineData("\"opaque\"", "\"token\"")]
    [InlineData("v1", "w2")]
    public void Opaque_or_prefix_shifted_tokens_fail_closed(string baseline, string current)
    {
        // Documented boundary: Tenants requires an ordered-token state store. A hash, GUID or shifted
        // prefix cannot prove causal advancement, so confirmation must withhold rather than guess.
        TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeFalse();
    }

    [Fact]
    public void Advancement_requires_event_evidence_from_the_tracked_command()
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement("1", "2", hasCommandEventEvidence: false)
            .ShouldBeFalse();

    [Fact]
    public void Legacy_to_tenant_sequence_migration_requires_event_evidence_from_the_tracked_command()
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement("opaque-etag", TenantProjectionVersionFormat.SequencePrefix + "42", hasCommandEventEvidence: false)
            .ShouldBeFalse();

    [Theory]
    [InlineData(null, "2")]
    [InlineData("1", null)]
    [InlineData("", "2")]
    [InlineData("   ", "2")]
    public void Missing_or_blank_versions_fail_closed(string? baseline, string? current)
        => TenantMembershipCommandProvenance
            .HasProjectionVersionAdvancement(baseline, current, hasCommandEventEvidence: true)
            .ShouldBeFalse();

    [Fact]
    public void Audit_provenance_admits_same_instant_evidence_as_documented()
    {
        DateTimeOffset attemptStarted = DateTimeOffset.Parse(
            "2026-08-20T10:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);

        TenantMembershipCommandProvenance
            .HasQualifyingAuditProvenance(attemptStarted, attemptStarted)
            .ShouldBeTrue();

        TenantMembershipCommandProvenance
            .HasQualifyingAuditProvenance(attemptStarted, attemptStarted.AddTicks(-1))
            .ShouldBeFalse();
    }
}
