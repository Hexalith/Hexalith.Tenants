using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>
/// Submits <c>BootstrapGlobalAdmin</c> for <c>Tenants:BootstrapGlobalAdminUserId</c> once the host is listening.
/// </summary>
/// <remarks>
/// EventStore Story 5.5: the command is authorized only by the configured administrator's delegated human credential
/// (see <see cref="TenantBootstrapCredentialProvider"/>), never by this service's Dapr application id. Without that
/// credential the command is not sent.
/// </remarks>
public partial class TenantBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TenantBootstrapOptions> options,
    IConfiguration configuration,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<TenantBootstrapHostedService> logger,
    TimeProvider? timeProvider = null) : IHostedService {
    private const string EventStoreAppId = "eventstore";
    private const string CommandEndpoint = "api/v1/commands";
    private const long MaxExpectedRejectionProbeBytes = 8192;

    public Task StartAsync(CancellationToken cancellationToken) {
        string? userId = options.Value.BootstrapGlobalAdminUserId;

        if (string.IsNullOrWhiteSpace(userId)) {
            Log.BootstrapSkipped(logger);
            return Task.CompletedTask;
        }

        if (cancellationToken.IsCancellationRequested) {
            return Task.CompletedTask;
        }

        // Defer until Kestrel is accepting requests — EventStore will invoke /process on
        // this service to handle the command, which requires the web host to be listening.
        //
        // Bind the deferred call to ApplicationStopping, NOT the StartAsync token: the StartAsync token is
        // only valid for the duration of host startup, and its source is torn down once startup completes
        // (right after ApplicationStarted fires). Capturing it for this post-startup HTTP call risks
        // cancelling the in-flight bootstrap request. ApplicationStopping cancels only on shutdown.
        CancellationToken stoppingToken = lifetime.ApplicationStopping;
        _ = lifetime.ApplicationStarted.Register(() =>
            _ = Task.Run(() => RunBootstrapAsync(userId, stoppingToken), stoppingToken));

        return Task.CompletedTask;
    }

    private async Task RunBootstrapAsync(string userId, CancellationToken cancellationToken) {
        try {
            AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false)) {
                DaprClient daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();
                IHttpClientFactory httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var command = new BootstrapGlobalAdmin(userId);
                JsonElement payloadElement = JsonSerializer.SerializeToElement(command);

                object commandBody = new {
                    messageId = UniqueIdHelper.GenerateSortableUniqueStringId(),
                    tenant = TenantIdentity.DefaultTenantId,
                    domain = TenantIdentity.GlobalAdministratorsDomain,
                    aggregateId = TenantIdentity.GlobalAdministratorsAggregateId,
                    commandType = nameof(BootstrapGlobalAdmin),
                    payload = payloadElement,
                    correlationId = UniqueIdHelper.GenerateSortableUniqueStringId(),
                };

                HttpClient httpClient = httpClientFactory.CreateClient();

                // The EventStore command endpoint requires the configured administrator's own delegated credential
                // (EventStore Story 5.5): no app-id grant exists. Dapr service invocation relays the Authorization
                // header to the eventstore app. Without a credential nothing is sent.
                var credentialProvider = new TenantBootstrapCredentialProvider(
                    configuration,
                    environment,
                    timeProvider ?? TimeProvider.System,
                    logger);
                string? accessToken = await credentialProvider.AcquireAsync(userId, httpClient, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(accessToken)) {
                    return;
                }

                using HttpRequestMessage httpRequest = daprClient.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    EventStoreAppId,
                    CommandEndpoint);
                httpRequest.Content = JsonContent.Create(commandBody);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode == HttpStatusCode.Accepted) {
                    Log.BootstrapCommandSent(logger);
                    return;
                }

                string errorBody = await ReadExpectedRejectionProbeAsync(httpResponse.Content, cancellationToken).ConfigureAwait(false);

                // Bootstrap is idempotent at the domain level. The GlobalAdminAlreadyBootstrappedRejection
                // type in a 409 body means the global admin was already registered (typical on every restart
                // after the first successful run). Other status codes must remain unexpected even if their
                // support-safe body mentions the marker.
                if (httpResponse.StatusCode == HttpStatusCode.Conflict
                    && errorBody.Contains("GlobalAdminAlreadyBootstrappedRejection", StringComparison.Ordinal)) {
                    Log.BootstrapAlreadyDone(logger);
                    return;
                }

                Log.BootstrapUnexpectedResponse(logger, (int)httpResponse.StatusCode);
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

    private static async Task<string> ReadExpectedRejectionProbeAsync(HttpContent content, CancellationToken cancellationToken) {
        try {
            await content.LoadIntoBufferAsync(MaxExpectedRejectionProbeBytes, cancellationToken).ConfigureAwait(false);
            return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException) {
            return string.Empty;
        }
        catch (InvalidOperationException) {
            return string.Empty;
        }
    }

    private static partial class Log {
        [LoggerMessage(
            EventId = 2000,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: Tenants:BootstrapGlobalAdminUserId is not configured")]
        public static partial void BootstrapSkipped(ILogger logger);

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Bootstrap command sent for configured global administrator")]
        public static partial void BootstrapCommandSent(ILogger logger);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Warning,
            Message = "Bootstrap unexpected response: StatusCode={StatusCode}")]
        public static partial void BootstrapUnexpectedResponse(ILogger logger, int statusCode);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Bootstrap failed — the global administrator may not have been created. The service will retry on next restart")]
        public static partial void BootstrapFailed(ILogger logger, Exception ex);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: initial global administrator is already registered")]
        public static partial void BootstrapAlreadyDone(ILogger logger);
    }
}
