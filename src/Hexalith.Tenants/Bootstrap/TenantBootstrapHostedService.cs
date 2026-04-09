using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Bootstrap;

public partial class TenantBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TenantBootstrapOptions> options,
    ILogger<TenantBootstrapHostedService> logger) : IHostedService {
    private const string EventStoreAppId = "eventstore";
    private const string CommandEndpoint = "api/v1/commands";

    public async Task StartAsync(CancellationToken cancellationToken) {
        string? userId = options.Value.BootstrapGlobalAdminUserId;

        if (string.IsNullOrWhiteSpace(userId)) {
            Log.BootstrapSkipped(logger);
            return;
        }

        try {
            AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false)) {
                DaprClient daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();
                IHttpClientFactory httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var command = new BootstrapGlobalAdmin(userId);
                JsonElement payloadElement = JsonSerializer.SerializeToElement(command);

                object commandBody = new {
                    messageId = Guid.NewGuid().ToString(),
                    tenant = TenantIdentity.DefaultTenantId,
                    domain = TenantIdentity.Domain,
                    aggregateId = "global-administrators",
                    commandType = nameof(BootstrapGlobalAdmin),
                    payload = payloadElement,
                    correlationId = Guid.NewGuid().ToString(),
                };

                using HttpRequestMessage httpRequest = daprClient.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    EventStoreAppId,
                    CommandEndpoint);
                httpRequest.Content = JsonContent.Create(commandBody);

                HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode == HttpStatusCode.Accepted) {
                    Log.BootstrapCommandSent(logger, userId);
                    return;
                }

                string errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Log.BootstrapUnexpectedResponse(logger, (int)httpResponse.StatusCode, errorBody);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            Log.BootstrapFailed(logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static partial class Log {
        [LoggerMessage(
            EventId = 2000,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: Tenants:BootstrapGlobalAdminUserId is not configured")]
        public static partial void BootstrapSkipped(ILogger logger);

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Bootstrap command sent for global administrator: UserId={UserId}")]
        public static partial void BootstrapCommandSent(ILogger logger, string userId);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Information,
            Message = "Bootstrap unexpected response: StatusCode={StatusCode}, Body={Body}")]
        public static partial void BootstrapUnexpectedResponse(ILogger logger, int statusCode, string body);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Bootstrap failed — the global administrator may not have been created. The service will retry on next restart")]
        public static partial void BootstrapFailed(ILogger logger, Exception ex);
    }
}
