using System.Reflection;

using Aspire.Hosting;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Supplies the missing stop phase at the frozen harness's existing client-disposal boundary.</summary>
internal static class FrozenAspireLifecycle
{
    private const string ShutdownClientName = "__tenants_frozen_shutdown__";

    internal static FrozenAspireShutdownClient Attach(AspireTopologyFixture fixture)
    {
        Type frozen = typeof(AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost>);
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo? application = frozen.GetField("_app", fields);
        FieldInfo? clients = frozen.GetField("_clients", fields);
        if (frozen.Assembly.GetName().Version != new Version(3, 128, 4, 0)
            || application?.FieldType != typeof(DistributedApplication)
            || clients?.FieldType != typeof(Dictionary<string, HttpClient>)
            || clients.GetValue(fixture) is not Dictionary<string, HttpClient> ownedClients)
        {
            throw new InvalidOperationException("frozen-aspire-lifecycle-contract-mismatch");
        }

        // The exact 3.128.4 disposer is final, including calls from its startup catch.
        // It disposes owned clients before the application, builder and fixture lock.
        // Attaching here covers that internal path without copying generic startup or
        // finding any application outside this one fixture's private application field.
        var shutdown = new FrozenAspireShutdownClient(cancellationToken =>
            application.GetValue(fixture) is DistributedApplication owned
                ? owned.StopAsync(cancellationToken)
                : Task.CompletedTask);
        if (!ownedClients.TryAdd(ShutdownClientName, shutdown))
        {
            throw new InvalidOperationException("frozen-aspire-lifecycle-already-attached");
        }

        return shutdown;
    }
}
