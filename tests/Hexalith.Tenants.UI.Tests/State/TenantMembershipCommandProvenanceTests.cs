using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>
/// Pins the projection-version contract membership confirmation depends on.
/// </summary>
/// <remarks>
/// Confirmation requires an ordered advancement of the value the query path publishes as
/// <c>ProjectionVersion</c>, which in production is the normalized Dapr state-store ETag
/// (<c>TenantQueryResult</c> falls back to it, and the read model never assigns its own).
/// The configured <c>state.redis</c> component issues per-key numeric versions, so the ordered
/// comparison holds. A store whose ETag is a hash or GUID does not satisfy it, and these tests
/// state that boundary explicitly so the requirement fails loudly rather than silently parking
/// every membership command in <c>ProjectionPending</c>.
/// </remarks>
public sealed class TenantMembershipCommandProvenanceTests
{
    [Theory]
    [InlineData("1", "2")]
    [InlineData("41", "42")]
    [InlineData("tenant-etag-1", "tenant-etag-2")]
    [InlineData("projection-v9", "projection-v10")]
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
