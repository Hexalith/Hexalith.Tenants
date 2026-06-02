namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection definition for sharing the Tenants DAPR test fixture across integration tests.
/// All tests decorated with [Collection("TenantsDaprTest")] share the same DAPR sidecar process.
/// </summary>
[CollectionDefinition("TenantsDaprTest", DisableParallelization = true)]
public sealed class TenantsDaprTestCollection : ICollectionFixture<TenantsDaprTestFixture>;
