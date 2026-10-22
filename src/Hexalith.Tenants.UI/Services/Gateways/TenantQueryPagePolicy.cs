namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Normalizes page bounds shared by the typed transport and query composition.</summary>
internal static class TenantQueryPagePolicy
{
    /// <summary>Clamps a requested page size to the supported service range, defaulting absent intent.</summary>
    /// <param name="pageSize">The caller-supplied page size.</param>
    /// <returns>The requested summary count accepted by the service.</returns>
    internal static int NormalizePageSize(int pageSize)
        => pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize,
        };
}
