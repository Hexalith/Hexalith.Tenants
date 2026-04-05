using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit fixture that boots the full Aspire AppHost topology
/// (CommandApi + Sample with DAPR sidecars) and creates HTTP clients for smoke tests.
/// Implements <see cref="IAsyncLifetime"/> for xUnit lifecycle management.
/// </summary>
public class AspireTopologyFixture : IAsyncLifetime {
    private static readonly int PlacementPort = OperatingSystem.IsWindows() ? 6050 : 50005;
    private static readonly int SchedulerPort = OperatingSystem.IsWindows() ? 6060 : 50006;
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CommandApiHealthTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SampleHealthTimeout = TimeSpan.FromSeconds(45);

    private DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _builder;
    private HttpClient? _commandApiClient;
    private HttpClient? _sampleClient;
    private readonly Stopwatch _startupStopwatch = new();
    private HttpStatusCode? _commandApiLastStatus;
    private string? _commandApiLastError;
    private HttpStatusCode? _sampleLastStatus;
    private string? _sampleLastError;

    /// <summary>
    /// Gets the HTTP client for the CommandApi service.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public HttpClient CommandApiClient => _commandApiClient ?? throw new InvalidOperationException(
        "Test infrastructure not initialized. Ensure InitializeAsync has completed.");

    /// <summary>
    /// Gets the HTTP client for the Sample service.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public HttpClient SampleClient => _sampleClient ?? throw new InvalidOperationException(
        "Test infrastructure not initialized. Ensure InitializeAsync has completed.");

    /// <inheritdoc/>
    public async Task InitializeAsync() {
        _startupStopwatch.Start();

        // 3-minute timeout: DAPR actor placement service registration takes time.
        using var startupCts = new CancellationTokenSource(StartupTimeout);

        try {
            await VerifyPrerequisitesAsync().ConfigureAwait(false);

            _builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>()
                .ConfigureAwait(false);

            _app = await _builder.BuildAsync().ConfigureAwait(false);
            await _app.StartAsync(startupCts.Token).ConfigureAwait(false);

            // Create HTTP clients for both resources.
            _commandApiClient = _app.CreateHttpClient("eventstore");
            _commandApiClient.Timeout = TimeSpan.FromSeconds(60);

            _sampleClient = _app.CreateHttpClient("sample");
            _sampleClient.Timeout = TimeSpan.FromSeconds(30);

            // Wait for CommandApi /health to return 200 OK.
            await WaitForHealthAsync(_commandApiClient, "eventstore", CommandApiHealthTimeout, CancellationToken.None).ConfigureAwait(false);

            // Wait for Sample /health to return 200 OK.
            await WaitForHealthAsync(_sampleClient, "sample", SampleHealthTimeout, CancellationToken.None).ConfigureAwait(false);
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

    /// <inheritdoc/>
    public async Task DisposeAsync() {
        _commandApiClient?.Dispose();
        _sampleClient?.Dispose();

        if (_app is not null) {
            await _app.DisposeAsync().ConfigureAwait(false);
        }

        if (_builder is not null) {
            await _builder.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task WaitForHealthAsync(HttpClient client, string resourceName, TimeSpan timeout, CancellationToken cancellationToken) {
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(timeout);

        while (!probeCts.Token.IsCancellationRequested) {
            try {
                using HttpResponseMessage response = await client
                    .GetAsync("/health", probeCts.Token)
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
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && !probeCts.Token.IsCancellationRequested) {
                SetHealthDiagnostics(resourceName, null, ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), probeCts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Resource '{resourceName}' did not become healthy within {timeout}. {GetHealthDiagnostic(resourceName)}");
    }

    private static async Task VerifyPrerequisitesAsync() {
        var failures = new List<string>();

        if (!await IsPortReachableAsync("localhost", PlacementPort).ConfigureAwait(false)) {
            failures.Add($"Dapr placement service is not reachable on localhost:{PlacementPort}");
        }

        if (!await IsPortReachableAsync("localhost", SchedulerPort).ConfigureAwait(false)) {
            failures.Add($"Dapr scheduler service is not reachable on localhost:{SchedulerPort}");
        }

        if (failures.Count > 0) {
            throw new InvalidOperationException(
                "Aspire topology prerequisites are missing. Have you run 'dapr init'?" + Environment.NewLine
                + string.Join(Environment.NewLine, failures.Select(f => $"  - {f}")));
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

    private void SetHealthDiagnostics(string resourceName, HttpStatusCode? status, string? error) {
        if (string.Equals(resourceName, "eventstore", StringComparison.Ordinal)) {
            _commandApiLastStatus = status;
            _commandApiLastError = error;
            return;
        }

        _sampleLastStatus = status;
        _sampleLastError = error;
    }

    private string GetHealthDiagnostic(string resourceName)
        => string.Equals(resourceName, "eventstore", StringComparison.Ordinal)
            ? $"Last status: {_commandApiLastStatus?.ToString() ?? "n/a"}, Last error: {_commandApiLastError ?? "n/a"}"
            : $"Last status: {_sampleLastStatus?.ToString() ?? "n/a"}, Last error: {_sampleLastError ?? "n/a"}";

    private string BuildTimeoutDiagnostics() {
        try {
            if (_app is null) {
                return "Application did not start (builder or build phase failed).";
            }

            return $"Resources expected: commandapi, sample. "
                + $"Startup duration: {_startupStopwatch.Elapsed}. "
                + $"commandapi => {GetHealthDiagnostic("eventstore")}. "
                + $"sample => {GetHealthDiagnostic("sample")}.";
        }
        catch (Exception ex) {
            return $"Failed to capture diagnostics: {ex.Message}";
        }
    }
}
