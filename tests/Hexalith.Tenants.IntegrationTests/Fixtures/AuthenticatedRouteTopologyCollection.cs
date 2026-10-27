namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Isolates the authenticated REST topology from worker-enabled background delivery fixtures.</summary>
[CollectionDefinition("AuthenticatedRouteTopology", DisableParallelization = true)]
public sealed class AuthenticatedRouteTopologyCollection : ICollectionFixture<AuthenticatedRouteTopologyFixture>;
