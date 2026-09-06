using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed class TenantQueryResultTests
{
    private static readonly JsonElement Payload = JsonSerializer.SerializeToElement(new { tenantId = "tenant.alpha" });
    private static readonly ReadModelFreshnessThresholds Thresholds = new(
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30));

    [Theory]
    [InlineData("opaque-etag", "opaque-etag")]
    [InlineData("  opaque-etag  ", "opaque-etag")]
    [InlineData("\"opaque-etag\"", "opaque-etag")]
    [InlineData("  \"opaque-etag\"  ", "opaque-etag")]
    [InlineData("W/\"abc\"", "W/\"abc\"")]
    public void Validator_only_factory_normalizes_opaque_etag(
        string eTag,
        string expectedETag)
    {
        TenantQueryResult result = TenantQueryResult.FromPayload(Payload, "tenants", eTag);

        AssertValidatorOnly(result, expectedETag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("  \" \"  ")]
    public void Validator_only_factory_omits_metadata_for_degenerate_etag(string? eTag)
    {
        TenantQueryResult result = TenantQueryResult.FromPayload(Payload, "tenants", eTag);

        result.Metadata.ShouldBeNull();
    }

    [Theory]
    [InlineData("2026-06-25T13:00:00Z", TenantProjectionVersionFormat.SequencePrefix + "42")]
    [InlineData("2026-06-25T12:00:00Z", null)]
    [InlineData(null, TenantProjectionVersionFormat.SequencePrefix + "42")]
    public void Freshness_overload_ignores_timestamp_and_sequence_authority(
        string? projectedAt,
        string? projectionVersion)
    {
        var readModel = new TenantReadModel
        {
            TenantId = "tenant.alpha",
            ProjectedAt = projectedAt is null ? null : DateTimeOffset.Parse(projectedAt, System.Globalization.CultureInfo.InvariantCulture),
            ProjectionVersion = projectionVersion,
        };

        TenantQueryResult result = TenantQueryResult.FromPayload(
            Payload,
            "tenants",
            readModel,
            Thresholds,
            DateTimeOffset.Parse("2026-06-25T13:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            "\"opaque-store-etag\"");

        AssertValidatorOnly(result, "opaque-store-etag");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("  \" \"  ")]
    public void Freshness_overload_omits_metadata_for_degenerate_etag(string? eTag)
    {
        var readModel = new TenantReadModel
        {
            TenantId = "tenant.alpha",
            ProjectedAt = DateTimeOffset.Parse("2026-06-25T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            ProjectionVersion = TenantProjectionVersionFormat.SequencePrefix + "42",
        };

        TenantQueryResult result = TenantQueryResult.FromPayload(
            Payload,
            "tenants",
            readModel,
            Thresholds,
            DateTimeOffset.Parse("2026-06-25T13:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            eTag);

        result.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Validator_only_factory_rejects_undefined_payload()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => TenantQueryResult.FromPayload(default, "tenants", "opaque-etag"));

        exception.ParamName.ShouldBe("payload");
        exception.Message.ShouldContain("Undefined");
    }

    [Fact]
    public void Freshness_overload_rejects_undefined_payload()
    {
        var readModel = new TenantReadModel
        {
            TenantId = "tenant.alpha",
            ProjectedAt = DateTimeOffset.Parse("2026-06-25T13:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            ProjectionVersion = TenantProjectionVersionFormat.SequencePrefix + "42",
        };

        ArgumentException exception = Should.Throw<ArgumentException>(
            () => TenantQueryResult.FromPayload(
                default,
                "tenants",
                readModel,
                Thresholds,
                DateTimeOffset.Parse("2026-06-25T13:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                "opaque-etag"));

        exception.ParamName.ShouldBe("payload");
        exception.Message.ShouldContain("Undefined");
    }

    private static void AssertValidatorOnly(TenantQueryResult result, string expectedETag)
    {
        result.Success.ShouldBeTrue();
        QueryResponseMetadata metadata = result.Metadata.ShouldNotBeNull();
        metadata.ETag.ShouldBe(expectedETag);
        metadata.IsNotModified.ShouldBe(false);
        metadata.ProjectionVersion.ShouldBeNull();
        metadata.IsStale.ShouldBeNull();
        metadata.IsDegraded.ShouldBeNull();
        metadata.ServedAt.ShouldBeNull();
        metadata.Provenance.ShouldBe(QueryResponseProvenance.Unknown);
        metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }
}
