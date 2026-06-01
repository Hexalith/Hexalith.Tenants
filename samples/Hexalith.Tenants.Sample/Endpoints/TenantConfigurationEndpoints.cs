using Hexalith.Tenants.Client.Projections;

namespace Hexalith.Tenants.Sample.Endpoints;

/// <summary>
/// Minimal API endpoints demonstrating namespace-filtered tenant configuration reads from the local projection.
/// </summary>
public static class TenantConfigurationEndpoints {
    private const string SampleNamespacePrefix = "sample.";

    /// <summary>
    /// Maps sample configuration endpoints that query the local tenant projection.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapTenantConfigurationEndpoints(this IEndpointRouteBuilder endpoints) {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapGet("/configuration/{tenantId}/sample", GetSampleConfigurationAsync);
        return endpoints;
    }

    /// <summary>
    /// Reads sample-owned configuration keys from the local tenant projection.
    /// </summary>
    public static async Task<IResult> GetSampleConfigurationAsync(
        string tenantId,
        ITenantProjectionStore store,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(tenantId)) {
            return Results.BadRequest(new { Message = "tenantId is required" });
        }

        TenantLocalState? state = await store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (state is null) {
            return Results.NotFound(new { TenantId = tenantId, Message = "Tenant not found in local projection" });
        }

        Dictionary<string, string> configuration = state.Configuration
            .Where(static pair => pair.Key.StartsWith(SampleNamespacePrefix, StringComparison.Ordinal))
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(
                static pair => pair.Key[SampleNamespacePrefix.Length..],
                static pair => pair.Value,
                StringComparer.Ordinal);

        return Results.Ok(new { TenantId = tenantId, Configuration = configuration });
    }
}
