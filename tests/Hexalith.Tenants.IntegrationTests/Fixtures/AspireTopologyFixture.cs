using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit fixture that boots the full Aspire AppHost topology
/// (CommandApi + Sample with DAPR sidecars) and creates HTTP clients for smoke tests.
/// Implements <see cref="IAsyncLifetime"/> for xUnit lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// This fixture verifies <strong>process liveness</strong>, not full readiness. It waits for
/// resources to reach <c>Running</c> state and for the <c>/alive</c> endpoint to return
/// HTTP 200 — that proves the host is responding to HTTP, not that every dependency
/// (database connections, Dapr sidecars, downstream services) is ready to serve traffic.
/// </para>
/// <para>
/// Full Dapr readiness (placement registration, sidecar handshake, state-store availability)
/// is covered by Dapr-specific integration tests that exercise the actor/state pipeline
/// end-to-end, not by this liveness smoke check.
/// </para>
/// </remarks>
public class AspireTopologyFixture : IAsyncLifetime {
    private static readonly int PlacementPort = OperatingSystem.IsWindows() ? 6050 : 50005;
    private static readonly int SchedulerPort = OperatingSystem.IsWindows() ? 6060 : 50006;

    // The Redis prerequisite targets the `dapr init`-managed Redis (which DAPR sidecars use as
    // their state-store and pub-sub backend). dapr init defaults to localhost:6379, but
    // developers can run dapr init on a non-default port; HEXALITH_TENANTS_TEST_REDIS_PORT
    // lets them override the probe port without editing the fixture. This probe does NOT
    // target an Aspire-managed Redis: the AppHost does not currently manage its own Redis
    // resource. If the AppHost ever takes over Redis management, switch this probe to read
    // the dynamically allocated port from the Aspire resource configuration instead.
    private static readonly int RedisPort = ResolveRedisPort();
    private const int DefaultRedisPort = 6379;
    private static readonly TimeSpan DockerProbeTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CommandApiHealthTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SampleHealthTimeout = TimeSpan.FromSeconds(45);
    private const string AlivenessEndpointPath = "/alive";

    private DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _builder;
    private HttpClient? _commandApiClient;
    private HttpClient? _tenantsClient;
    private HttpClient? _sampleClient;
    private FileStream? _daprFixtureLock;
    private readonly Stopwatch _startupStopwatch = new();
    private HttpStatusCode? _commandApiLastStatus;
    private string? _commandApiLastError;
    private HttpStatusCode? _tenantsLastStatus;
    private string? _tenantsLastError;
    private HttpStatusCode? _sampleLastStatus;
    private string? _sampleLastError;

    /// <summary>
    /// Gets the HTTP client for the CommandApi service.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public HttpClient CommandApiClient {
        get {
            SkipIfUnavailable();
            return _commandApiClient ?? throw new InvalidOperationException(
                "Test infrastructure not initialized. Ensure InitializeAsync has completed.");
        }
    }

    /// <summary>
    /// Gets the HTTP client for the Tenants domain service (exposes /process endpoint).
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public HttpClient TenantsClient {
        get {
            SkipIfUnavailable();
            return _tenantsClient ?? throw new InvalidOperationException(
                "Test infrastructure not initialized. Ensure InitializeAsync has completed.");
        }
    }

    /// <summary>
    /// Gets the HTTP client for the Sample service.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public HttpClient SampleClient {
        get {
            SkipIfUnavailable();
            return _sampleClient ?? throw new InvalidOperationException(
                "Test infrastructure not initialized. Ensure InitializeAsync has completed.");
        }
    }

    /// <summary>
    /// Gets a value indicating whether local DAPR prerequisites were available during fixture startup.
    /// </summary>
    public bool PrerequisitesAvailable { get; private set; } = true;

    /// <summary>
    /// Gets the skip reason when local DAPR prerequisites are unavailable.
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync() {
        AcquireDaprFixtureLock();
        _startupStopwatch.Start();

        // 3-minute timeout: DAPR actor placement service registration takes time.
        using var startupCts = new CancellationTokenSource(StartupTimeout);

        try {
            IReadOnlyList<string> prerequisiteFailures = await GetPrerequisiteFailuresAsync().ConfigureAwait(false);
            if (prerequisiteFailures.Count > 0) {
                PrerequisitesAvailable = false;
                SkipReason = BuildPrerequisiteFailureMessage(prerequisiteFailures);
                _startupStopwatch.Stop();
                return;
            }

            _builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>(new[] { "--EnableKeycloak=false" }, startupCts.Token)
                .ConfigureAwait(false);

            // Honor StartupTimeout during the build/graph-evaluation phase as well; MSBuild
            // hangs during project graph evaluation would otherwise never trip the timeout.
            _app = await _builder.BuildAsync(startupCts.Token).ConfigureAwait(false);

            await _app.StartAsync(startupCts.Token).ConfigureAwait(false);

            // Create HTTP clients for all resources. Clients are built through Aspire's
            // _app.CreateHttpClient(resourceName, endpointName) so service-discovery and the
            // DelegatingHandler chain remain attached, and HttpClient Timeout is configured
            // inline at construction time rather than mutated after first use.
            _commandApiClient = await WaitForResourceAndCreateClientAsync(
                "eventstore", "http", TimeSpan.FromSeconds(60), CommandApiHealthTimeout, startupCts.Token).ConfigureAwait(false);

            _tenantsClient = await WaitForResourceAndCreateClientAsync(
                "tenants", "http", TimeSpan.FromSeconds(60), CommandApiHealthTimeout, startupCts.Token).ConfigureAwait(false);

            _sampleClient = await WaitForResourceAndCreateClientAsync(
                "sample", "http", TimeSpan.FromSeconds(30), SampleHealthTimeout, startupCts.Token).ConfigureAwait(false);

            // Wait for process liveness. Full Dapr readiness is covered by Dapr-specific integration tests.
            await WaitForEndpointAsync(_commandApiClient, "eventstore", AlivenessEndpointPath, CommandApiHealthTimeout, CancellationToken.None).ConfigureAwait(false);

            // Wait for process liveness. Full Dapr readiness is covered by Dapr-specific integration tests.
            await WaitForEndpointAsync(_tenantsClient, "tenants", AlivenessEndpointPath, CommandApiHealthTimeout, CancellationToken.None).ConfigureAwait(false);

            // Wait for process liveness. Full Dapr readiness is covered by Dapr-specific integration tests.
            await WaitForEndpointAsync(_sampleClient, "sample", AlivenessEndpointPath, SampleHealthTimeout, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (startupCts.IsCancellationRequested) {
            _startupStopwatch.Stop();
            string diagnostics = BuildTimeoutDiagnostics();
            await DisposeAsync().ConfigureAwait(false);
            throw new TimeoutException(
                $"Aspire topology did not start within {StartupTimeout}. Startup ran for {_startupStopwatch.Elapsed}.{Environment.NewLine}{diagnostics}");
        }
        catch {
            _startupStopwatch.Stop();
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _startupStopwatch.Stop();
    }

    /// <summary>
    /// Skips the current test when local DAPR prerequisites were not available during fixture startup.
    /// </summary>
    public void SkipIfUnavailable() {
        if (!PrerequisitesAvailable) {
            Assert.Skip(SkipReason ?? DaprTestPrerequisites.SkipReason);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
        _commandApiClient?.Dispose();
        _tenantsClient?.Dispose();
        _sampleClient?.Dispose();

        if (_app is not null) {
            await _app.DisposeAsync().ConfigureAwait(false);
        }

        if (_builder is not null) {
            await _builder.DisposeAsync().ConfigureAwait(false);
        }

        _daprFixtureLock?.Dispose();
    }

    private void AcquireDaprFixtureLock() {
        string lockPath = Path.Combine(Path.GetTempPath(), "hexalith-tenants-dapr-fixture.lock");
        while (true) {
            try {
                _daprFixtureLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return;
            }
            catch (IOException) {
                Thread.Sleep(250);
            }
        }
    }

    private async Task WaitForEndpointAsync(HttpClient client, string resourceName, string endpointPath, TimeSpan timeout, CancellationToken cancellationToken) {
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(timeout);

        while (!probeCts.Token.IsCancellationRequested) {
            try {
                using HttpResponseMessage response = await client
                    .GetAsync(endpointPath, probeCts.Token)
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK) {
                    SetHealthDiagnostics(resourceName, response.StatusCode, null);
                    return;
                }

                SetHealthDiagnostics(resourceName, response.StatusCode, null);
            }
            catch (HttpRequestException ex) {
                SetHealthDiagnostics(resourceName, null, ex.Message);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                SetHealthDiagnostics(resourceName, null, ex.Message);
                if (probeCts.Token.IsCancellationRequested) {
                    break;
                }
            }

            try {
                await Task.Delay(TimeSpan.FromSeconds(2), probeCts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) {
                break;
            }
        }

        throw new TimeoutException(
            $"Resource '{resourceName}' endpoint '{endpointPath}' did not return HTTP 200 within {timeout}. {GetHealthDiagnostic(resourceName)}");
    }

    private async Task<HttpClient> WaitForResourceAndCreateClientAsync(
        string resourceName,
        string endpointName,
        TimeSpan clientTimeout,
        TimeSpan readinessTimeout,
        CancellationToken cancellationToken) {
        if (_app is null) {
            throw new InvalidOperationException("Aspire application has not been built.");
        }

        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(readinessTimeout);

        try {
            await _app.ResourceNotifications
                .WaitForResourceAsync(resourceName, KnownResourceStates.Running, probeCts.Token)
                .ConfigureAwait(false);

            // WaitForResourceAsync(Running) returns before endpoint URLs are guaranteed to be
            // published. Poll Snapshot.Urls until the named endpoint appears (or the readiness
            // timeout fires) — this avoids the misleading "did not expose endpoint" error that
            // the previous one-shot snapshot check raised on a URL-publication race.
            UrlSnapshot endpoint = await WaitForEndpointPublishedAsync(
                resourceName, endpointName, probeCts.Token).ConfigureAwait(false);

            // UrlSnapshot.Url can be null; throw a descriptive error rather than letting
            // new Uri(null!) surface a generic ArgumentNullException.
            if (string.IsNullOrWhiteSpace(endpoint.Url)) {
                throw new InvalidOperationException(
                    $"Resource '{resourceName}' published endpoint '{endpointName}' but its URL value is null or whitespace.");
            }

            // Use Aspire's CreateHttpClient so service-discovery handlers, retry policies, and
            // tracing DelegatingHandlers stay attached; set Timeout in the same statement so it
            // is in effect before the first request is issued.
            HttpClient client = _app.CreateHttpClient(resourceName, endpointName);
            client.BaseAddress ??= new Uri(endpoint.Url);
            client.Timeout = clientTimeout;
            return client;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            string state = _app.ResourceNotifications.TryGetCurrentState(resourceName, out ResourceEvent? current)
                ? current.Snapshot.State?.Text ?? "n/a"
                : "n/a";

            throw new TimeoutException(
                $"Resource '{resourceName}' did not reach Running with endpoint '{endpointName}' published within {readinessTimeout}. Last state: {state}.");
        }
    }

    private async Task<UrlSnapshot> WaitForEndpointPublishedAsync(
        string resourceName, string endpointName, CancellationToken cancellationToken) {
        if (_app is null) {
            throw new InvalidOperationException("Aspire application has not been built.");
        }

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();

            if (_app.ResourceNotifications.TryGetCurrentState(resourceName, out ResourceEvent? resourceEvent)) {
                UrlSnapshot? endpoint = resourceEvent.Snapshot.Urls
                    .FirstOrDefault(url => string.Equals(url.Name, endpointName, StringComparison.OrdinalIgnoreCase));

                if (endpoint is not null) {
                    return endpoint;
                }
            }

            try {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
        }
    }

    private static async Task<IReadOnlyList<string>> GetPrerequisiteFailuresAsync() {
        var failures = new List<string>();

        if (!IsDockerHealthy()) {
            failures.Add("Docker is not running or is not healthy enough for Aspire container orchestration");
        }

        if (!await IsRedisResponsiveAsync().ConfigureAwait(false)) {
            failures.Add($"Redis is not responding to PING on localhost:{RedisPort}");
        }

        if (!await IsPortReachableAsync("localhost", PlacementPort).ConfigureAwait(false)) {
            failures.Add($"Dapr placement service is not reachable on localhost:{PlacementPort}");
        }

        if (!await IsPortReachableAsync("localhost", SchedulerPort).ConfigureAwait(false)) {
            failures.Add($"Dapr scheduler service is not reachable on localhost:{SchedulerPort}");
        }

        return failures;
    }

    private static string BuildPrerequisiteFailureMessage(IReadOnlyList<string> failures)
        => "Aspire topology prerequisites are missing. Start Docker Desktop and run 'dapr init' before running these tests." + Environment.NewLine
            + string.Join(Environment.NewLine, failures.Select(f => $"  - {f}"));

    private static bool IsDockerHealthy() {
        try {
            using var process = Process.Start(new ProcessStartInfo {
                FileName = "docker",
                Arguments = "info --format \"{{.ServerVersion}}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null) {
                return false;
            }

            if (!process.WaitForExit(DockerProbeTimeout)) {
                try {
                    process.Kill(entireProcessTree: true);
                }
                catch {
                    // Best-effort cleanup for a hung Docker CLI probe.
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch {
            return false;
        }
    }

    private static async Task<bool> IsPortReachableAsync(string host, int port) {
        try {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            return true;
        }
        catch {
            return false;
        }
    }

    private static int ResolveRedisPort() {
        string? overrideValue = Environment.GetEnvironmentVariable("HEXALITH_TENANTS_TEST_REDIS_PORT");
        if (!string.IsNullOrWhiteSpace(overrideValue)
            && int.TryParse(overrideValue, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
            && parsed is > 0 and < 65536) {
            return parsed;
        }

        return DefaultRedisPort;
    }

    private static async Task<bool> IsRedisResponsiveAsync() {
        try {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.ConnectAsync("localhost", RedisPort, cts.Token).ConfigureAwait(false);
            await using NetworkStream stream = client.GetStream();
            byte[] ping = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");
            await stream.WriteAsync(ping, cts.Token).ConfigureAwait(false);

            byte[] buffer = new byte[16];
            int total = 0;
            while (total < 5) {
                int chunk = await stream.ReadAsync(buffer.AsMemory(total), cts.Token).ConfigureAwait(false);
                if (chunk <= 0) {
                    break;
                }

                total += chunk;
            }

            return total >= 5 && Encoding.ASCII.GetString(buffer, 0, total).StartsWith("+PONG", StringComparison.Ordinal);
        }
        catch {
            return false;
        }
    }

    private void SetHealthDiagnostics(string resourceName, HttpStatusCode? status, string? error) {
        if (string.Equals(resourceName, "eventstore", StringComparison.Ordinal)) {
            _commandApiLastStatus = status;
            _commandApiLastError = error;
            return;
        }

        if (string.Equals(resourceName, "tenants", StringComparison.Ordinal)) {
            _tenantsLastStatus = status;
            _tenantsLastError = error;
            return;
        }

        _sampleLastStatus = status;
        _sampleLastError = error;
    }

    private string GetHealthDiagnostic(string resourceName)
        => resourceName switch {
            "eventstore" => $"Last status: {_commandApiLastStatus?.ToString() ?? "n/a"}, Last error: {_commandApiLastError ?? "n/a"}",
            "tenants" => $"Last status: {_tenantsLastStatus?.ToString() ?? "n/a"}, Last error: {_tenantsLastError ?? "n/a"}",
            _ => $"Last status: {_sampleLastStatus?.ToString() ?? "n/a"}, Last error: {_sampleLastError ?? "n/a"}",
        };

    private string BuildTimeoutDiagnostics() {
        try {
            if (_app is null) {
                return "Application did not start (builder or build phase failed).";
            }

            return $"Resources expected: eventstore, tenants, sample. "
                + $"Startup duration: {_startupStopwatch.Elapsed}. "
                + $"eventstore => {GetHealthDiagnostic("eventstore")}. "
                + $"tenants => {GetHealthDiagnostic("tenants")}. "
                + $"sample => {GetHealthDiagnostic("sample")}.";
        }
        catch (Exception ex) {
            return $"Failed to capture diagnostics: {ex.Message}";
        }
    }
}
