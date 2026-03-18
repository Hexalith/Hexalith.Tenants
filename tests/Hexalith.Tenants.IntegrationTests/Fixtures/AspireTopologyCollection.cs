namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection definition that shares a single <see cref="AspireTopologyFixture"/>
/// across all Aspire topology test classes. This starts the full Aspire topology
/// (CommandApi, Sample, DAPR sidecars) ONCE instead of per-class.
/// </summary>
[CollectionDefinition("AspireTopology")]
public class AspireTopologyCollection : ICollectionFixture<AspireTopologyFixture>;
