using System.Text.Json;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public sealed class TenantIndexProjectionVersionTests
{
    [Fact]
    public void CanonicalOrderAndObservationMetadataDoNotChangeAuthority()
    {
        TenantIndexReadModel model = CompleteIndex();
        string version = TenantIndexProjectionVersion.Create(model).ShouldNotBeNull();
        model.Tenants = model.Tenants.Reverse().ToDictionary();
        model.UserTenants = model.UserTenants.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value.Reverse().ToDictionary());
        model.AggregateSequences = model.AggregateSequences.Reverse().ToDictionary();
        model.ProjectedAt = DateTimeOffset.UtcNow;
        model.ProjectionVersion = "untrusted-old-version";
        TenantIndexProjectionVersion.Create(model).ShouldBe(version);
        version.ShouldStartWith("tenant-index:sha256:");
    }

    [Theory]
    [InlineData("name")]
    [InlineData("status")]
    [InlineData("tenant")]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("membership")]
    [InlineData("watermark")]
    [InlineData("empty-owner")]
    public void EverySemanticAndOwnerWatermarkChangeChangesAuthority(string change)
    {
        TenantIndexReadModel model = CompleteIndex();
        string? before = TenantIndexProjectionVersion.Create(model);
        switch (change)
        {
            case "name": model.Tenants["alpha"] = model.Tenants["alpha"] with { Name = "Changed" }; break;
            case "status": model.Tenants["alpha"] = model.Tenants["alpha"] with { Status = TenantStatus.Disabled }; break;
            case "tenant": model.Tenants.Remove("beta"); break;
            case "user": model.UserTenants["new"] = new() { ["alpha"] = TenantRole.TenantReader }; break;
            case "role": model.UserTenants["reader"]["alpha"] = TenantRole.TenantOwner; break;
            case "membership": model.UserTenants["reader"].Remove("alpha"); break;
            case "watermark": model.AggregateSequences["alpha"]++; break;
            case "empty-owner": model.AggregateSequences["rejection-only"] = 1; break;
        }
        TenantIndexProjectionVersion.Create(model).ShouldNotBeNull().ShouldNotBe(before);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("membership")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("empty")]
    [InlineData("null-tenant")]
    public void IncompleteLegacyCoverageDoesNotManufactureAuthority(string missing)
    {
        TenantIndexReadModel model = CompleteIndex();
        switch (missing)
        {
            case "tenant": model.AggregateSequences.Remove("beta"); break;
            case "membership": model.UserTenants["reader"]["orphan"] = TenantRole.TenantReader; break;
            case "zero": model.AggregateSequences["alpha"] = 0; break;
            case "negative": model.AggregateSequences["alpha"] = -1; break;
            case "empty": model = new(); break;
            case "null-tenant": model.Tenants["alpha"] = null!; break;
        }
        TenantIndexProjectionVersion.Create(model).ShouldBeNull();
    }

    private static TenantIndexReadModel CompleteIndex() => new()
    {
        Tenants = new() { ["alpha"] = new("Alpha", TenantStatus.Active), ["beta"] = new("Beta", TenantStatus.Active) },
        UserTenants = new() { ["reader"] = new() { ["alpha"] = TenantRole.TenantReader, ["beta"] = TenantRole.TenantOwner } },
        AggregateSequences = new() { ["alpha"] = 2, ["beta"] = 4 },
    };
}
