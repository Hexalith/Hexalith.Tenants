namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Qualifies authenticated REST requests using the existing request-path projection profile.</summary>
/// <remarks>The shared worker-enabled topology and its background event-delivery assertions remain independent.</remarks>
public sealed class AuthenticatedRouteTopologyFixture : AspireTopologyFixture, IAsyncLifetime
{
    /// <inheritdoc/>
    public new ValueTask InitializeAsync() => WithRequestPathSettingsAsync(() => base.InitializeAsync());

    /// <summary>Scopes the inherited child-process profile to startup and restores it even when startup fails.</summary>
    internal static async ValueTask WithRequestPathSettingsAsync(Func<ValueTask> initialize)
    {
        const string key = "EventStore__Projections__WorkersEnabled";
        string? previous = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "false");
            await initialize();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }
}
