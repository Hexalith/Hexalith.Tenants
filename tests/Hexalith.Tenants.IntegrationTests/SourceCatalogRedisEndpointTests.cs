#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Preserves the CI default and rejects non-loopback, credential, and multi-endpoint substitutions.</summary>
public sealed class SourceCatalogRedisEndpointTests
{
    /// <summary>The existing CI profile retains its original endpoint and bounded connection settings.</summary>
    [Fact]
    public void MissingOverridePreservesTheExistingCiBackend()
    {
        var options = SourceCatalogBootstrapEvidence.RedisOptions(null);
        options.EndPoints.Single().ToString().ShouldBe("Unspecified/localhost:6379");
        options.AbortOnConnectFail.ShouldBeFalse();
        options.ConnectTimeout.ShouldBe(5000);
        options.AsyncTimeout.ShouldBe(5000);
    }

    /// <summary>Only explicit numeric loopback endpoints can select an independently owned local backend.</summary>
    [Theory]
    [InlineData("127.0.0.1:49152")]
    [InlineData("[::1]:49153")]
    public void ExplicitLoopbackEndpointPreservesTheSelectedPort(string endpoint)
        => SourceCatalogBootstrapEvidence.RedisOptions(endpoint).EndPoints.Single().ToString().ShouldBe(endpoint);

    /// <summary>Unknown, external, credential-bearing and multi-endpoint values remain unavailable.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("127.0.0.1:0")]
    [InlineData("192.0.2.1:6379")]
    [InlineData("redis.example:6379")]
    [InlineData("127.0.0.1:6379,password=not-a-real-secret")]
    [InlineData("127.0.0.1:6379,127.0.0.1:6380")]
    [InlineData("redis://127.0.0.1:6379")]
    public void SubstitutedEndpointRefusesWithoutEchoingTheValue(string endpoint)
        => Should.Throw<InvalidOperationException>(() => SourceCatalogBootstrapEvidence.RedisOptions(endpoint))
            .Message.ShouldBe("source-catalog-backend-endpoint-invalid");
}
#endif
