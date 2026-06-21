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

public partial class TenantBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TenantBootstrapOptions> options,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<TenantBootstrapHostedService> logger) : IHostedService {
    private const string EventStoreAppId = "eventstore";
    private const string CommandEndpoint = "api/v1/commands";
    private const long MaxExpectedRejectionProbeBytes = 8192;

    public Task StartAsync(CancellationToken cancellationToken) {
        string? userId = options.Value.BootstrapGlobalAdminUserId;

        if (string.IsNullOrWhiteSpace(userId)) {
            Log.BootstrapSkipped(logger);
            return Task.CompletedTask;
        }

        // Defer until Kestrel is accepting requests — EventStore will invoke /process on
        // this service to handle the command, which requires the web host to be listening.
        _ = lifetime.ApplicationStarted.Register(() =>
            _ = Task.Run(() => RunBootstrapAsync(userId, cancellationToken), cancellationToken));

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

                using HttpRequestMessage httpRequest = daprClient.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    EventStoreAppId,
                    CommandEndpoint);
                httpRequest.Content = JsonContent.Create(commandBody);

                HttpClient httpClient = httpClientFactory.CreateClient();

                // The EventStore command endpoint requires a JWT. The bootstrap runs without a user
                // context, so acquire a service token via Keycloak (resource-owner-password grant) and
                // forward it as a Bearer header — Dapr service invocation relays the Authorization header
                // to the eventstore app. Without a token the command is rejected 401 (MissingToken).
                string? accessToken = await TryAcquireAccessTokenAsync(httpClient, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(accessToken)) {
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode == HttpStatusCode.Accepted) {
                    Log.BootstrapCommandSent(logger);
                    return;
                }

                string errorBody = await ReadExpectedRejectionProbeAsync(httpResponse.Content, cancellationToken).ConfigureAwait(false);

                // Bootstrap is idempotent at the domain level. A 409 with the
                // GlobalAdminAlreadyBootstrappedRejection type means the global admin was
                // already registered (typical on every restart after the first successful run).
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

    // Resource-owner-password grant against Keycloak using the configured service credentials,
    // mirroring how the EventStore Admin UI obtains its service token. Returns null when no
    // authority is configured (non-Keycloak/dev signing-key mode), letting the caller send the
    // command without a bearer header.
    private async Task<string?> TryAcquireAccessTokenAsync(HttpClient httpClient, CancellationToken cancellationToken) {
        string? authority = configuration["EventStore:Authentication:Authority"];
        string? username = configuration["EventStore:Authentication:Username"];
        string? password = configuration["EventStore:Authentication:Password"];
        string clientId = configuration["EventStore:Authentication:ClientId"] ?? "hexalith-eventstore";

        if (string.IsNullOrWhiteSpace(authority)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)) {
            return null;
        }

        using var form = new FormUrlEncodedContent(new Dictionary<string, string> {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["username"] = username,
            ["password"] = password,
        });

        string tokenEndpoint = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";
        using HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, form, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            Log.BootstrapTokenRequestFailed(logger, (int)response.StatusCode);
            return null;
        }

        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false)) {
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return document.RootElement.TryGetProperty("access_token", out JsonElement token)
                ? token.GetString()
                : null;
        }
    }

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

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Warning,
            Message = "Bootstrap service token request failed: StatusCode={StatusCode}. The bootstrap command will be sent unauthenticated and likely rejected")]
        public static partial void BootstrapTokenRequestFailed(ILogger logger, int statusCode);
    }
}
