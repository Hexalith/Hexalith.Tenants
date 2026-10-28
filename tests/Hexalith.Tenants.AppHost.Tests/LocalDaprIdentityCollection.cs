namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Isolates shared temporary-directory cleanup assertions from other AppHost compositions.</summary>
[CollectionDefinition("LocalDaprIdentity", DisableParallelization = true)]
public sealed class LocalDaprIdentityCollection;
