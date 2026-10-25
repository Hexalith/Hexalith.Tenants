namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Isolates process-environment controls without creating a live topology fixture.</summary>
[CollectionDefinition("BootstrapReadiness", DisableParallelization = true)]
public sealed class AspireBootstrapEnvironmentCollection;
