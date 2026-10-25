namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Keeps authenticated live-route state separate from the anonymous topology lane.</summary>
[CollectionDefinition("AuthenticatedTenantsTopology", DisableParallelization = true)]
public sealed class AuthenticatedTenantsTopologyCollection : ICollectionFixture<AuthenticatedTenantsTopologyFixture>;
