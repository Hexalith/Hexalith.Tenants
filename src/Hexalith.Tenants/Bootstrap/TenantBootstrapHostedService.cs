using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
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
/// credential the command is not sent. One attempt is made per host start; after a failure operators must verify
/// command status and projected membership before an explicit recovery (see docs/production-auth-readiness.md).
/// </remarks>
public partial class TenantBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TenantBootstrapOptions> options,
    IConfiguration configuration,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<TenantBootstrapHostedService> logger,
    TimeProvider? timeProvider = null) : IHostedService
{
    private const string EventStoreAppId = "eventstore";
    private const string CommandEndpoint = "api/v1/commands";
    private const long MaxExpectedRejectionProbeBytes = 8192;
    private const string DomainRejectionProblemPrefix = "https://hexalith.io/problems/domain-rejections/";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.BootstrapGlobalAdminEnabled)
        {
            Log.BootstrapDisabled(logger);
            return Task.CompletedTask;
        }

        string? userId = options.Value.BootstrapGlobalAdminUserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            Log.BootstrapSkipped(logger);
            return Task.CompletedTask;
        }

        if (cancellationToken.IsCancellationRequested)
        {
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

    private async Task RunBootstrapAsync(string userId, CancellationToken cancellationToken)
    {
        string phase = "initialization";
        try
        {
            AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                DaprClient daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();
                IHttpClientFactory httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var command = new BootstrapGlobalAdmin(userId);
                JsonElement payloadElement = JsonSerializer.SerializeToElement(command);

                string messageId = UniqueIdHelper.GenerateSortableUniqueStringId();
                string correlationId = UniqueIdHelper.GenerateSortableUniqueStringId();
                object commandBody = new
                {
                    messageId,
                    tenant = TenantIdentity.DefaultTenantId,
                    domain = TenantIdentity.GlobalAdministratorsDomain,
                    aggregateId = TenantIdentity.GlobalAdministratorsAggregateId,
                    commandType = nameof(BootstrapGlobalAdmin),
                    payload = payloadElement,
                    correlationId,
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
                phase = "credential-acquisition";
                // Password forms must never be replayed by an automatic redirect. Command invocation keeps
                // the host's existing default transport, independently of this credential-only policy.
                HttpClient credentialClient = httpClientFactory.CreateClient(TenantBootstrapServiceCollectionExtensions.CredentialClientName);
                string? accessToken = await credentialProvider.AcquireAsync(userId, credentialClient, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return;
                }

                HttpRequestMessage? selectedRequest;
                if (configuration.GetValue<bool>("Tenants:BootstrapBoundWorkloadEnabled"))
                {
                    phase = "bound-administrator-validation";
                    var submitted = new SubmitCommandRequest(messageId, TenantIdentity.DefaultTenantId,
                        TenantIdentity.GlobalAdministratorsDomain, TenantIdentity.GlobalAdministratorsAggregateId,
                        nameof(BootstrapGlobalAdmin), payloadElement, correlationId);
                    selectedRequest = await TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration,
                        daprClient, submitted, userId, accessToken, timeProvider ?? TimeProvider.System,
                        httpClient.Timeout, cancellationToken).ConfigureAwait(false);
                    if (selectedRequest is null) return;
                }
                else
                {
                    selectedRequest = daprClient.CreateInvokeMethodRequest(HttpMethod.Post, EventStoreAppId, CommandEndpoint);
                    selectedRequest.Content = JsonContent.Create(commandBody);
                    selectedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                using HttpRequestMessage httpRequest = selectedRequest;

                TenantBootstrapReceipt? receipt = configuration.GetValue<bool>("Tenants:BootstrapBoundWorkloadEnabled")
                    ? await TenantBootstrapReceipt.BeginAsync(configuration, messageId, correlationId, userId, cancellationToken).ConfigureAwait(false) : null;
                phase = "command-dispatch";
                Log.BootstrapAttempt(logger, messageId, correlationId);
                using HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    if (receipt is not null) await receipt.CompleteAsync(configuration, (int)httpResponse.StatusCode, "accepted", cancellationToken).ConfigureAwait(false);
                    Log.BootstrapCommandSent(logger);
                    return;
                }

                phase = "command-response";
                string errorBody = await ReadExpectedRejectionProbeAsync(httpResponse.Content, cancellationToken).ConfigureAwait(false);

                // Bootstrap is idempotent at the domain level. The GlobalAdminAlreadyBootstrappedRejection
                // type in a 409 body means the global admin was already registered (typical on every restart
                // after the first successful run). Other status codes must remain unexpected even if their
                // support-safe body mentions the marker.
                if (httpResponse.StatusCode == HttpStatusCode.Conflict
                    && errorBody.Contains("GlobalAdminAlreadyBootstrappedRejection", StringComparison.Ordinal))
                {
                    if (receipt is not null) await receipt.CompleteAsync(configuration, (int)httpResponse.StatusCode, "already-bootstrapped", cancellationToken).ConfigureAwait(false);
                    Log.BootstrapAlreadyDone(logger);
                    return;
                }

                if (receipt is not null) await receipt.CompleteAsync(configuration, (int)httpResponse.StatusCode, "response-received", cancellationToken).ConfigureAwait(false);
                (string responseKind, string responseCode) = ClassifyUnexpectedResponse(errorBody);
                Log.BootstrapUnexpectedResponse(logger, (int)httpResponse.StatusCode, responseKind, responseCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // HTTP and token-parser exception details may contain credentials, bearer tokens, or authority URLs.
            Log.BootstrapFailed(logger, "bootstrap-operation-failed", phase, ex.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<string> ReadExpectedRejectionProbeAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            if (content.Headers.ContentLength is > MaxExpectedRejectionProbeBytes) return string.Empty;
            await content.LoadIntoBufferAsync(MaxExpectedRejectionProbeBytes, cancellationToken).ConfigureAwait(false);
            string body = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            // Already-buffered content can bypass LoadIntoBufferAsync's limit. Keep the
            // diagnostic/parser input bounded even when the transport supplied a buffer.
            return body.Length <= MaxExpectedRejectionProbeBytes && Encoding.UTF8.GetByteCount(body) <= MaxExpectedRejectionProbeBytes
                ? body : string.Empty;
        }
        catch (HttpRequestException)
        {
            return string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    // This is diagnostic classification only: neither response fields nor error detail are authority.
    // Return closed literals exclusively; credentials and arbitrary URLs may appear in any body field.
    private static (string Kind, string Code) ClassifyUnexpectedResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return ("unavailable", "none");
        try
        {
            using JsonDocument document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 16 });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return ("json-other", "none");
            if (root.TryGetProperty("errorCode", out JsonElement error))
            {
                return ("dapr", error.ValueKind == JsonValueKind.String ? error.GetString() switch
                {
                    "ERR_DIRECT_INVOKE" => "ERR_DIRECT_INVOKE",
                    "ERR_PERMISSION_DENIED" => "ERR_PERMISSION_DENIED",
                    "ERR_ACTOR_INVOKE_METHOD" => "ERR_ACTOR_INVOKE_METHOD",
                    "ERR_ACTOR_INSTANCE_MISSING" => "ERR_ACTOR_INSTANCE_MISSING",
                    "ERR_ACTOR_RUNTIME_NOT_FOUND" => "ERR_ACTOR_RUNTIME_NOT_FOUND",
                    "ERR_ACTOR_NO_ADDRESS" => "ERR_ACTOR_NO_ADDRESS",
                    "ERR_ACTOR_NO_PLACEMENT" => "ERR_ACTOR_NO_PLACEMENT",
                    "ERR_STATE_SAVE" => "ERR_STATE_SAVE",
                    _ => "unrecognized",
                } : "unrecognized");
            }
            string kind = root.TryGetProperty("type", out JsonElement type) ? type.ValueKind == JsonValueKind.String ? type.GetString() switch
            {
                "https://hexalith.io/problems/validation-error" => "problem-validation-error",
                "https://hexalith.io/problems/bad-request" => "problem-bad-request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1" => "problem-bad-request",
                "https://hexalith.io/problems/forbidden" => "problem-forbidden",
                "https://hexalith.io/problems/domain-rejections" => "problem-domain-rejection",
                "https://hexalith.io/problems/idempotency-admission-failure" => "problem-idempotency-admission",
                "https://hexalith.io/problems/command-identity-conflict" => "problem-command-identity",
                "https://hexalith.io/problems/concurrency-conflict" => "problem-concurrency",
                "https://hexalith.io/problems/service-unavailable" => "problem-service-unavailable",
                "https://hexalith.io/problems/internal-server-error" => "problem-internal-error",
                "https://hexalith.io/problems/authentication-required" => "problem-authentication-required",
                "https://hexalith.io/problems/token-expired" => "problem-token-expired",
                "https://hexalith.io/problems/not-found" => "problem-not-found",
                "https://hexalith.io/problems/not-implemented" => "problem-not-implemented",
                "https://hexalith.io/problems/unsupported-api-version" => "problem-unsupported-api-version",
                "https://hexalith.io/problems/idempotency-conflict" => "problem-idempotency-conflict",
                "https://hexalith.io/problems/idempotency-key-expired" => "problem-idempotency-key-expired",
                "https://hexalith.io/problems/command-correlation-ambiguous" => "problem-command-correlation-ambiguous",
                "https://hexalith.io/problems/rate-limit-exceeded" => "problem-rate-limit",
                "https://hexalith.io/problems/backpressure-exceeded" => "problem-backpressure",
                "https://hexalith.io/problems/command-status-not-found" => "problem-command-status-not-found",
                string uri when uri.Length > DomainRejectionProblemPrefix.Length
                    && uri.StartsWith(DomainRejectionProblemPrefix, StringComparison.Ordinal) => "problem-domain-rejection",
                _ => "problem-other",
            } : "problem-other" : "json-object";
            JsonElement code = root.TryGetProperty("reasonCode", out JsonElement reason) ? reason
                : root.TryGetProperty("code", out JsonElement canonical) ? canonical : default;
            return (kind, code.ValueKind == JsonValueKind.Undefined ? "none" : code.ValueKind == JsonValueKind.String ? code.GetString() switch
            {
                "authentication_required" => "authentication_required",
                "subject_missing" => "subject_missing",
                "tenant_missing" => "tenant_missing",
                "tenant_mismatch" => "tenant_mismatch",
                "tenant_not_found" => "tenant_not_found",
                "tenant_disabled" => "tenant_disabled",
                "tenant_suspended" => "tenant_suspended",
                "tenant_stale" => "tenant_stale",
                "tenant_unavailable" => "tenant_unavailable",
                "tenant_ambiguous" => "tenant_ambiguous",
                "principal_not_member" => "principal_not_member",
                "insufficient_role" => "insufficient_role",
                "insufficient_permission" => "insufficient_permission",
                "authorization_service_unavailable" => "authorization_service_unavailable",
                "channel-unconfigured" => "channel-unconfigured",
                "channel-token-missing" => "channel-token-missing",
                "channel-token-duplicate" => "channel-token-duplicate",
                "channel-token-invalid" => "channel-token-invalid",
                "assertion-missing" => "assertion-missing",
                "assertion-duplicate" => "assertion-duplicate",
                "credential-conflict" => "credential-conflict",
                "assertion-malformed" => "assertion-malformed",
                "assertion-expired" => "assertion-expired",
                "assertion-not-yet-valid" => "assertion-not-yet-valid",
                "assertion-stale" => "assertion-stale",
                "assertion-invalid" => "assertion-invalid",
                "audience-invalid" => "audience-invalid",
                "issuer-invalid" => "issuer-invalid",
                "signature-invalid" => "signature-invalid",
                "algorithm-invalid" => "algorithm-invalid",
                "caller-missing" => "caller-missing",
                "caller-ambiguous" => "caller-ambiguous",
                "caller-not-allowed" => "caller-not-allowed",
                "caller-conflict" => "caller-conflict",
                "operation-missing" => "operation-missing",
                "operation-not-granted" => "operation-not-granted",
                "binding-ambiguous" => "binding-ambiguous",
                "binding-mismatch" => "binding-mismatch",
                "binding-missing" => "binding-missing",
                "verifier-unconfigured" => "verifier-unconfigured",
                "verifier-unavailable" => "verifier-unavailable",
                "administrator-verifier-unavailable" => "administrator-verifier-unavailable",
                "idempotency_conflict" => "idempotency_conflict",
                "idempotency_key_expired" => "idempotency_key_expired",
                "idempotency_admission_rejected" => "idempotency_admission_rejected",
                "idempotency_admission_unavailable" => "idempotency_admission_unavailable",
                "idempotency_admission_corrupt" => "idempotency_admission_corrupt",
                "idempotency_key_collision" => "idempotency_key_collision",
                "idempotency_unsafe_legacy_state" => "idempotency_unsafe_legacy_state",
                "idempotency_reconciliation_unavailable" => "idempotency_reconciliation_unavailable",
                "idempotency_outcome_unknown" => "idempotency_outcome_unknown",
                _ => "unrecognized",
            } : "unrecognized");
        }
        catch (JsonException)
        {
            return ("non-json", "none");
        }
        catch (InvalidOperationException)
        {
            // JsonElement.GetString can reject malformed escaped Unicode after parsing.
            return ("non-json", "none");
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2008,
            Level = LogLevel.Information,
            Message = "Bootstrap command dispatch attempt: MessageId={MessageId}, CorrelationId={CorrelationId}. Retain these identifiers for status and projected membership recovery")]
        public static partial void BootstrapAttempt(ILogger logger, string messageId, string correlationId);

        [LoggerMessage(
            EventId = 2007,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: Tenants:BootstrapGlobalAdminEnabled is false")]
        public static partial void BootstrapDisabled(ILogger logger);

        [LoggerMessage(
            EventId = 2000,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: Tenants:BootstrapGlobalAdminUserId is not configured")]
        public static partial void BootstrapSkipped(ILogger logger);

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Bootstrap command accepted for configured global administrator; verify command status and projected membership before handoff")]
        public static partial void BootstrapCommandSent(ILogger logger);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Warning,
            Message = "Bootstrap unexpected response: StatusCode={StatusCode}, ResponseKind={ResponseKind}, ResponseCode={ResponseCode}. Verify command status and projected membership before explicit operator recovery")]
        public static partial void BootstrapUnexpectedResponse(ILogger logger, int statusCode, string responseKind, string responseCode);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Bootstrap failed: Reason={Reason}, Phase={Phase}, ExceptionType={ExceptionType}. The global administrator may not have been created. Verify command status and projected membership before explicit operator recovery; a restart makes one new attempt")]
        public static partial void BootstrapFailed(ILogger logger, string reason, string phase, string exceptionType);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: initial global administrator is already registered; verify configured administrator projected membership before handoff")]
        public static partial void BootstrapAlreadyDone(ILogger logger);
    }
}
