namespace Hexalith.Tenants.Configuration;

/// <summary>Selects the physical gateway without changing logical workload audiences or owner proof scope.</summary>
internal static class TenantEventStoreTarget
{
    /// <summary>Gets the configured Dapr target, retaining the canonical default.</summary>
    internal static string AppId(IConfiguration configuration)
    {
        string value = configuration["Tenants:EventStoreAppId"] ?? "eventstore";
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
