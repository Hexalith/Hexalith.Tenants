using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.UI.Services.Gateways;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit fixture that boots the full Tenants Aspire AppHost topology
/// (CommandApi + Sample with DAPR sidecars) and creates HTTP clients for smoke tests.
/// </summary>
/// <remarks>
/// All DAPR/Aspire plumbing — placement/scheduler endpoint resolution, prerequisite probing, build/
/// start, client creation, endpoint polling, and liveness diagnostics — lives in the reusable platform
/// base <see cref="AspireTopologyFixtureBase{TAppHost}"/>. This fixture supplies the Tenants resources
/// and requires projected caller authority after its explicit post-startup bootstrap.
/// </remarks>
public class AspireTopologyFixture : AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost>, IAsyncLifetime {
    internal const string DemoSigningKey = "DevOnlySigningKey-AtLeast32Chars!";
    private static readonly TimeSpan CommandApiHealthTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan TenantsApiHealthTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan SampleHealthTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CommandApiClientTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan TenantsApiClientTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SampleClientTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BootstrapReadinessTimeout = TimeSpan.FromMinutes(2);

    /// <summary>Uses the SDK's trusted development certificate directory without changing TLS validation.</summary>
    public AspireTopologyFixture()
    {
        ConfigureCertificateTrust();
    }

    /// <inheritdoc/>
    public new async ValueTask InitializeAsync()
    {
        await WithBootstrapStartupSettingsAsync(() => base.InitializeAsync());
        if (!PrerequisitesAvailable)
        {
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(BootstrapReadinessTimeout);
        string token = AspireTopologyTests.CreateDemoJwt();
        using HttpClient scoped = AspireTopologyTests.CreateIsolatedTenantsApiClient(TenantsApiClient, token, _ => { });
        var client = new TenantsRestQueryClient(scoped);
        await CompleteBootstrapAsync(CommandApiClient, token,
            (cursor, cancellationToken) => client.GetGlobalAdministratorsAsync(
                new GetGlobalAdministratorsQuery { PageSize = 100, Cursor = cursor }, null, cancellationToken),
            () => base.DisposeAsync(),
            string.Equals(Environment.GetEnvironmentVariable("TENANTS_LIVE_REQUIRE_COLD_BOOTSTRAP"), "true", StringComparison.OrdinalIgnoreCase),
            timeout.Token);
    }

    /// <summary>Runs the real bootstrap and authority checks behind the fixture's support-safe cleanup boundary.</summary>
    internal static async Task CompleteBootstrapAsync(
        HttpClient commandApi,
        string token,
        Func<string?, CancellationToken, Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>>> read,
        Func<ValueTask> cleanup,
        bool requireColdBootstrap,
        CancellationToken cancellationToken)
    {
        string phase = "bootstrap-command";
        string failure = "bootstrap-command-failed";
        try
        {
            (long? sequence, string? commandFailure) = await SubmitBootstrapAsync(commandApi, token, requireColdBootstrap, cancellationToken);
            if (commandFailure is not null)
            {
                failure = commandFailure;
                throw new InvalidOperationException();
            }

            phase = "bootstrap-authority";
            failure = "bootstrap-authority-unavailable";
            await WaitForBootstrapAuthorityAsync(read, sequence, requireColdBootstrap, cancellationToken);
        }
        catch
        {
            if (cancellationToken.IsCancellationRequested) failure = phase + "-timeout";
            try
            {
                await cleanup();
            }
            catch
            {
                failure += ":cleanup-failed";
            }

            // Neither transport/status exceptions nor disposal failures may retain credentials or URLs.
            throw new InvalidOperationException(failure);
        }
    }

    /// <summary>Applies inherited child-process settings only while the platform starts this fixture.</summary>
    internal static async ValueTask WithBootstrapStartupSettingsAsync(Func<ValueTask> initialize)
    {
        string[] keys = ["Tenants__BootstrapGlobalAdminEnabled", "EventStore__ProjectionDispatch__CatalogRefreshInterval"];
        string?[] previous = keys.Select(Environment.GetEnvironmentVariable).ToArray();
        try
        {
            Environment.SetEnvironmentVariable(keys[0], "false");
            Environment.SetEnvironmentVariable(keys[1], "00:00:01");
            await initialize();
        }
        finally
        {
            for (int index = 0; index < keys.Length; index++) Environment.SetEnvironmentVariable(keys[index], previous[index]);
        }
    }

    /// <summary>Traverses opaque production cursors until the caller has current authority at the actual committed prefix.</summary>
    internal static async Task WaitForBootstrapAuthorityAsync(
        Func<string?, CancellationToken, Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>>> read,
        long? committedSequence,
        bool requireColdBootstrap,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            string? cursor = null;
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> response;
                try
                {
                    response = await read(cursor, cancellationToken);
                }
                catch (HttpRequestException exception) when (IsTransientTransport(exception))
                {
                    break;
                }
                cancellationToken.ThrowIfCancellationRequested();
                QueryResponseMetadata metadata = response.Metadata;
                if (response.StatusCode != (int)HttpStatusCode.OK || response.FailureKind != TenantsRestQueryFailureKind.None
                    || response.Payload is not { } page
                    || metadata.Provenance != QueryResponseProvenance.ProjectionBacked
                    || metadata.Lifecycle != ProjectionLifecycleState.Current
                    || metadata.IsStale is not false || metadata.IsDegraded is true || metadata.IsNotModified is true
                    || string.IsNullOrWhiteSpace(metadata.ETag)
                    || !TenantAuditStreams.TryGetSequence(metadata.ProjectionVersion, out long sequence)
                    || sequence < (committedSequence ?? 1) || requireColdBootstrap && sequence != 1
                    || metadata.ServedAt is not { } servedAt || servedAt < DateTimeOffset.UtcNow.AddMinutes(-5)
                    || servedAt > DateTimeOffset.UtcNow.AddSeconds(5))
                {
                    break;
                }

                if (page.Items?.Any(item => item is not null && item.UserId == AuthenticatedRouteState.CallerId) == true) return;
                if (!page.HasMore || string.IsNullOrWhiteSpace(page.Cursor)) break;
                if (!cursors.Add(page.Cursor)) throw new InvalidOperationException("bootstrap-authority-cursor-cycle");
                cursor = page.Cursor;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    private static async Task<(long? Sequence, string? Failure)> SubmitBootstrapAsync(
        HttpClient commandApi, string token, bool requireColdBootstrap, CancellationToken cancellationToken)
    {
        // Retrying the same immutable request preserves the gateway's command/idempotency identity.
        var command = AspireTopologyTests.CreateCommand("global-administrators", "global-administrators",
            nameof(BootstrapGlobalAdmin), new BootstrapGlobalAdmin(AuthenticatedRouteState.CallerId));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commands") { Content = JsonContent.Create(command) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response;
            try
            {
                response = await commandApi.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException exception) when (IsTransientTransport(exception))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            using (response)
            {
                if (IsTransientStatus(response.StatusCode))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    byte[]? body = await ReadBoundedBootstrapBodyAsync(response.Content, cancellationToken);
                    if (body is null) return (null, "bootstrap-command-acceptance-invalid");
                    SubmitCommandResponse? accepted = JsonSerializer.Deserialize<SubmitCommandResponse>(body, JsonSerializerOptions.Web);
                    if (string.IsNullOrWhiteSpace(accepted?.CorrelationId)
                        || accepted.MessageId is not null && accepted.MessageId != command.MessageId)
                        return (null, "bootstrap-command-acceptance-invalid");

                    Hexalith.EventStore.Models.CommandStatusResponse status = await AspireTopologyTests.WaitForTerminalStatusAsync(
                        commandApi, accepted.CorrelationId, token, cancellationToken);
                    if (status.CorrelationId != accepted.CorrelationId
                        || status.MessageId is not null && status.MessageId != command.MessageId
                        || status.AggregateId != command.AggregateId
                        || status.Domain is not null && status.Domain != command.Domain)
                        return (null, "bootstrap-command-terminal-identity-invalid");
                    if (status.Status == "Completed" && status.StatusCode == (int)CommandStatus.Completed
                        && status.EventCount == 1 && status.CommittedEventSequence is > 0)
                    {
                        return requireColdBootstrap && status.CommittedEventSequence != 1
                            ? (null, "bootstrap-cold-state-required") : (status.CommittedEventSequence, null);
                    }
                    if (status.Status == "Rejected" && status.StatusCode == (int)CommandStatus.Rejected
                        && KnownBootstrapReason(status.RejectionEventType) == "already-bootstrapped")
                        return (null, requireColdBootstrap ? "bootstrap-cold-state-required" : null);
                    return (null, "bootstrap-command-terminal-invalid");
                }

                string reason = await BootstrapFailureReasonAsync(response.Content, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Conflict && reason == "already-bootstrapped")
                    return (null, requireColdBootstrap ? "bootstrap-cold-state-required" : null);
                return (null, $"bootstrap-command-http-{(int)response.StatusCode}:{reason}");
            }
        }
    }

    private static bool IsTransientStatus(HttpStatusCode status)
        => status is HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static bool IsTransientTransport(HttpRequestException exception)
        => exception.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError or HttpRequestError.ResponseEnded
            || exception.StatusCode is { } status && IsTransientStatus(status);

    /// <summary>Returns only known bootstrap failure categories from a truly bounded response stream.</summary>
    internal static async Task<string> BootstrapFailureReasonAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            byte[]? body = await ReadBoundedBootstrapBodyAsync(content, cancellationToken);
            if (body is null) return "unclassified";
            using JsonDocument problem = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 });
            if (problem.RootElement.ValueKind != JsonValueKind.Object) return "unclassified";
            string? rejection = problem.RootElement.TryGetProperty("rejectionType", out JsonElement type)
                ? KnownBootstrapReason(type.ValueKind == JsonValueKind.String ? type.GetString() : "invalid") : null;
            string? reason = problem.RootElement.TryGetProperty("reasonCode", out JsonElement code)
                ? KnownBootstrapReason(code.ValueKind == JsonValueKind.String ? code.GetString() : "invalid") : null;
            return rejection == "unclassified" || reason == "unclassified"
                || rejection is not null && reason is not null && rejection != reason
                ? "unclassified" : rejection ?? reason ?? "unclassified";
        }
        catch (Exception exception) when (exception is JsonException or HttpRequestException or InvalidOperationException or IOException)
        {
            return "unclassified";
        }
    }

    private static async Task<byte[]?> ReadBoundedBootstrapBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        // Content-Length and LoadIntoBufferAsync cannot enforce a bound for every HttpContent implementation.
        // One extra byte distinguishes a complete 8192-byte body from a truncated oversized body, even if prebuffered.
        const int limit = 8192;
        using Stream stream = await content.ReadAsStreamAsync(cancellationToken);
        byte[] bytes = new byte[limit + 1];
        int count = 0;
        while (count < bytes.Length)
        {
            int read = await stream.ReadAsync(bytes.AsMemory(count), cancellationToken);
            if (read == 0) return bytes[..count];
            count += read;
        }
        return null;
    }

    private static string KnownBootstrapReason(string? reason)
        => reason switch
        {
            "GlobalAdminAlreadyBootstrappedRejection" or "Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdminAlreadyBootstrappedRejection"
                or "global-admin-already-bootstrapped-rejection" => "already-bootstrapped",
            "InsufficientPermissionsRejection" or "Hexalith.Tenants.Contracts.Events.Rejections.InsufficientPermissionsRejection"
                or "insufficient-permissions-rejection" => "insufficient-permissions",
            _ => "unclassified",
        };

    /// <summary>Includes the SDK's user trust store alongside the system OpenSSL certificate directories.</summary>
    internal static void ConfigureCertificateTrust()
    {
        string userTrust = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "dev-certs", "trust");
        string configured = Environment.GetEnvironmentVariable("SSL_CERT_DIR") ?? "/usr/lib/ssl/certs";
        string[] directories = [userTrust, .. configured.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)];
        Environment.SetEnvironmentVariable("SSL_CERT_DIR", string.Join(Path.PathSeparator, directories.Distinct(StringComparer.Ordinal)));
    }

    /// <summary>Describes bounded transport categories without copying exception messages, URLs, or credentials.</summary>
    internal static string DescribeTransportFailure(HttpRequestException exception)
    {
        var categories = new List<string> { exception.HttpRequestError.ToString() };
        Exception? current = exception;
        for (int depth = 0; current is not null && depth < 4; depth++, current = current.InnerException)
        {
            categories.Add(current.GetType().Name);
            foreach (string cause in new[] { "UntrustedRoot", "PartialChain", "RemoteCertificateNameMismatch", "NotTimeValid" })
            {
                if (current.Message.Contains(cause, StringComparison.Ordinal))
                {
                    categories.Add(cause);
                }
            }
        }

        return string.Join(" / ", categories.Distinct(StringComparer.Ordinal));
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<AspireResource> Resources =>
    [
        new("eventstore", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: true, CommandApiHealthTimeout),
        new("tenants", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: true, CommandApiHealthTimeout),
        new("tenants-ui", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: false, CommandApiHealthTimeout),
        new("eventstore-dapr-cli", "http", TenantsApiClientTimeout, TenantsApiHealthTimeout, WaitForAliveness: false, TenantsApiHealthTimeout),
        new("tenants-ui-dapr-cli", "http", TenantsApiClientTimeout, TenantsApiHealthTimeout, WaitForAliveness: false, TenantsApiHealthTimeout),
        new("sample", "http", SampleClientTimeout, SampleHealthTimeout, WaitForAliveness: true, SampleHealthTimeout),
        // HTTPS is required because tenants-api redirects HTTP. Aliveness is polled by the Story 4.7
        // proof with that test's timeout so a slow or unhealthy generated API does not stall the
        // shared fixture's 6-minute startup budget or the other topology tests.
        new("tenants-api", "https", TenantsApiClientTimeout, TenantsApiHealthTimeout, WaitForAliveness: false, TenantsApiHealthTimeout),
    ];

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExtraAppArgs =>
    [
        "--EnableKeycloak=false",
        "--EnableMemories=false",
        "--Dapr:PlacementHostAddress=",
        "--Dapr:SchedulerHostAddress=",
        $"--Authentication:JwtBearer:SigningKey={DemoSigningKey}",
    ];

    /// <summary>Gets the HTTP client for the CommandApi (eventstore) service.</summary>
    public HttpClient CommandApiClient => Client("eventstore");

    /// <summary>Gets the HTTP client for the Tenants domain service (exposes /process endpoint).</summary>
    public HttpClient TenantsClient => Client("tenants");

    /// <summary>Gets the HTTP client for the generated Tenants REST API.</summary>
    public HttpClient TenantsApiClient => Client("tenants-api");

    /// <summary>Gets the HTTP client for the Tenants UI resource.</summary>
    public HttpClient TenantsUiClient => Client("tenants-ui");

    /// <summary>Gets the authenticated EventStore peer sidecar for receiver-denial controls.</summary>
    public HttpClient EventStoreDaprClient => Client("eventstore-dapr-cli");

    /// <summary>Gets the UI caller sidecar's HTTP endpoint for real service-invocation qualification.</summary>
    public HttpClient TenantsUiDaprClient => Client("tenants-ui-dapr-cli");

    /// <summary>Gets the HTTP client for the Sample service.</summary>
    public HttpClient SampleClient => Client("sample");
}
