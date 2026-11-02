using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Reads a finite catalog-readiness budget without changing the installed HTTP transport or resilience pipeline.</summary>
internal static class TenantBootstrapReadinessBudget
{
    /// <summary>
    /// The installed unnamed standard pipeline's public options name. The production-default registration
    /// test binds this value to <see cref="IHttpStandardResiliencePipelineBuilder.PipelineName"/>.
    /// </summary>
    internal const string UnnamedStandardPipelineOptionsName = "-standard";

    /// <summary>Keeps finite client timeouts and resolves the configured standard total timeout for an infinite transport.</summary>
    internal static TimeSpan Resolve(HttpClient client, IServiceProvider services)
    {
        TimeSpan selected = client.Timeout;
        if (selected == Timeout.InfiniteTimeSpan)
        {
            HttpStandardResilienceOptions options = services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
                .Get(UnnamedStandardPipelineOptionsName);
            if (options.TotalRequestTimeout.TimeoutGenerator is not null)
                throw new InvalidOperationException("Bootstrap catalog readiness requires a finite static transport budget.");
            selected = options.TotalRequestTimeout.Timeout;
        }
        if (selected <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(client), "Bootstrap catalog readiness requires a positive finite transport budget.");
        return selected;
    }
}
