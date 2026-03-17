using Hexalith.Tenants.Client.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Maps DAPR pub/sub subscription endpoint for tenant events.
/// </summary>
public static class TenantEventSubscriptionEndpoints
{
    /// <summary>
    /// Maps the DAPR pub/sub subscription endpoint for tenant events.
    /// Consuming services call this to subscribe to tenant event notifications.
    /// Requires <c>app.UseCloudEvents()</c> and <c>app.MapSubscribeHandler()</c> to be configured.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapTenantEventSubscription(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        HexalithTenantsOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;

        endpoints.MapPost("/tenants/events", async (
            TenantEventEnvelope envelope,
            TenantEventProcessor processor,
            CancellationToken cancellationToken) =>
        {
            TenantEventProcessingResult result = await processor.ProcessAsync(envelope, cancellationToken).ConfigureAwait(false);
            return result switch
            {
                TenantEventProcessingResult.Processed => Results.Ok(),
                TenantEventProcessingResult.Duplicate => Results.Ok(),
                TenantEventProcessingResult.SkippedUnknownEventType => Results.Ok(),
                TenantEventProcessingResult.SkippedNoHandlers => Results.Ok(),
                TenantEventProcessingResult.FailedInvalidPayload => Results.Problem(
                    title: "Tenant event processing failed.",
                    detail: "The tenant event payload could not be deserialized.",
                    statusCode: StatusCodes.Status500InternalServerError),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
            };
        }).WithTopic(options.PubSubName, options.TopicName);

        return endpoints;
    }
}
