using System.Net;

using Dapr;
using Dapr.Client;

using Grpc.Core;

using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Observes the configured sidecar's identity and actual inbound app-health admission before bootstrap effects.</summary>
internal static class TenantBootstrapAppReadiness
{
    /// <summary>Waits for a read-only self invocation under the caller's original finite readiness deadline.</summary>
    internal static async Task WaitAsync(IServiceProvider services, DaprClient dapr, HttpClient probeClient, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        DomainProjectionIdentityOptions identity = services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value;
        identity.Validate();
        string appId = identity.AppId;
        if (appId is "." or ".." || !string.Equals(appId, Uri.EscapeDataString(appId), StringComparison.Ordinal))
            throw new InvalidOperationException("Bootstrap self identity must be a single application-id segment.");

        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                DaprMetadata metadata = await dapr.GetMetadataAsync(token).WaitAsync(token).ConfigureAwait(false);
                if (metadata is null || !string.Equals(metadata.Id, appId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Bootstrap sidecar identity does not match the domain service.");
                break;
            }
            catch (Exception error) when (IsTransientTransport(error)) { }
            await Task.Delay(100, token).ConfigureAwait(false);
        }

        while (true)
        {
            token.ThrowIfCancellationRequested();
            using HttpRequestMessage request = dapr.CreateInvokeMethodRequest(HttpMethod.Get, appId, "alive");
            if (request.Method != HttpMethod.Get || request.Content is not null
                || request.RequestUri is not { IsAbsoluteUri: true } uri
                || uri.AbsolutePath != $"/v1.0/invoke/{appId}/method/alive"
                || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
                throw new InvalidOperationException("Bootstrap readiness requires the exact read-only self invocation.");
            try
            {
                using HttpResponseMessage response = await probeClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (response.IsSuccessStatusCode) return;
                if (response.StatusCode is not (HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable))
                    throw new HttpRequestException("Bootstrap self health refused readiness.", null, response.StatusCode);
            }
            catch (Exception error) when (IsTransientTransport(error)) { }
            await Task.Delay(100, token).ConfigureAwait(false);
        }
    }

    /// <summary>Limits retries to read-only connection failures and the SDK's explicit unavailable metadata transport.</summary>
    private static bool IsTransientTransport(Exception error)
        => error is HttpRequestException
        {
            StatusCode: null,
            HttpRequestError: HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError or HttpRequestError.ResponseEnded,
        }
        or RpcException { StatusCode: StatusCode.Unavailable }
        or DaprException { InnerException: RpcException { StatusCode: StatusCode.Unavailable } };
}
