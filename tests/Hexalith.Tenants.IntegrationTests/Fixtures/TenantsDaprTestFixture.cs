using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.CommandApi.DomainProcessing;
using Hexalith.Tenants.Server.Aggregates;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Integration test fixture that starts the Tenants CommandApi with a local daprd sidecar,
/// reusing the DAPR infrastructure (Redis, placement, scheduler) from dapr init.
/// Tests the full command pipeline: Actor → Domain Service Invocation → /process → Aggregate → Events.
/// </summary>
public sealed class TenantsDaprTestFixture : IAsyncLifetime {
    private const string AppId = "commandapi";
    private static readonly int PlacementPort = OperatingSystem.IsWindows() ? 6050 : 50005;
    private static readonly int SchedulerPort = OperatingSystem.IsWindows() ? 6060 : 50006;
    private const int RedisPort = 6379;
    private const int HealthTimeoutSeconds = 60;

    private Process? _daprProcess;
    private WebApplication? _testHost;
    private int _appPort;
    private int _daprHttpPort;
    private int _daprGrpcPort;
    private int _daprInternalGrpcPort;
    private int _daprMetricsPort;
    private int _daprProfilePort;
    private string? _componentsDir;

    private string? _previousDaprHttpPort;
    private string? _previousDaprGrpcPort;
    private readonly StringBuilder _daprStdout = new();
    private readonly StringBuilder _daprStderr = new();

    /// <summary>Gets the Dapr HTTP endpoint for actor proxy clients.</summary>
    public string DaprHttpEndpoint => $"http://localhost:{_daprHttpPort}";

    /// <summary>Gets the application HTTP endpoint (used to force actor deactivation in tests).</summary>
    public string AppEndpoint => $"http://localhost:{_appPort}";

    /// <summary>Gets the fake event publisher for capturing published events.</summary>
    public FakeEventPublisher EventPublisher { get; } = new();

    /// <summary>Gets the fake dead-letter publisher for test assertions.</summary>
    public FakeDeadLetterPublisher DeadLetterPublisher { get; } = new();

    /// <summary>Gets the in-memory command status store for tracking command lifecycle.</summary>
    public InMemoryCommandStatusStore CommandStatusStore { get; } = new();

    /// <inheritdoc/>
    public async Task InitializeAsync() {
        KillOrphanedDaprdProcesses();

        int[] ports = GetAvailablePorts(6);
        _appPort = ports[0];
        _daprHttpPort = ports[1];
        _daprGrpcPort = ports[2];
        _daprInternalGrpcPort = ports[3];
        _daprMetricsPort = ports[4];
        _daprProfilePort = ports[5];

        _previousDaprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
        _previousDaprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT");
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", _daprHttpPort.ToString());
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", _daprGrpcPort.ToString());

        await VerifyPrerequisitesAsync().ConfigureAwait(false);

        _componentsDir = CreateComponentFiles();

        await StartTestHostAsync().ConfigureAwait(false);

        await VerifyAppListeningAsync().ConfigureAwait(false);

        StartDaprSidecar();

        await WaitForDaprHealthAsync().ConfigureAwait(false);

        // Let sidecar complete actor registration with placement service.
        await Task.Delay(2000).ConfigureAwait(false);

        await VerifyAppListeningAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() {
        if (_testHost is not null) {
            await _testHost.StopAsync().ConfigureAwait(false);
            await _testHost.DisposeAsync().ConfigureAwait(false);
        }

        if (_daprProcess is not null && !_daprProcess.HasExited) {
            _daprProcess.Kill(entireProcessTree: true);
            await _daprProcess.WaitForExitAsync().ConfigureAwait(false);
        }

        _daprProcess?.Dispose();

        if (_componentsDir is not null && Directory.Exists(_componentsDir)) {
            try {
                Directory.Delete(_componentsDir, recursive: true);
            }
            catch {
                // Best-effort cleanup
            }
        }

        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", _previousDaprHttpPort);
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", _previousDaprGrpcPort);
    }

    private static async Task VerifyPrerequisitesAsync() {
        var failures = new List<string>();

        if (!await IsPortReachableAsync("localhost", RedisPort).ConfigureAwait(false)) {
            failures.Add($"Redis is not reachable on localhost:{RedisPort}");
        }

        if (!await IsPortReachableAsync("localhost", PlacementPort).ConfigureAwait(false)) {
            failures.Add($"Dapr placement service is not reachable on localhost:{PlacementPort}");
        }

        if (!await IsPortReachableAsync("localhost", SchedulerPort).ConfigureAwait(false)) {
            failures.Add($"Dapr scheduler service is not reachable on localhost:{SchedulerPort}");
        }

        if (failures.Count > 0) {
            throw new InvalidOperationException(
                $"Dapr infrastructure pre-flight check failed. Have you run 'dapr init'?\n" +
                string.Join("\n", failures.Select(f => $"  - {f}")));
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

    private async Task StartTestHostAsync() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions());

        // Configure DAPR ports for actor runtime
        builder.Configuration["DAPR_HTTP_PORT"] = _daprHttpPort.ToString();
        builder.Configuration["DAPR_GRPC_PORT"] = _daprGrpcPort.ToString();
        builder.Configuration["Dapr:HttpPort"] = _daprHttpPort.ToString();
        builder.Configuration["Dapr:GrpcPort"] = _daprGrpcPort.ToString();

        // Configure domain service registration: system|tenants|v1 → self (commandapi)
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:AppId"] = AppId;
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:MethodName"] = "process";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:TenantId"] = "system";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Domain"] = "tenants";
        builder.Configuration["EventStore:DomainServices:Registrations:system|tenants|v1:Version"] = "v1";

        // Configure pub/sub name for event publisher
        builder.Configuration["EventStore:Publisher:PubSubName"] = "pubsub";

        _ = builder.WebHost.ConfigureKestrel(serverOptions =>
            serverOptions.ListenLocalhost(_appPort, listenOptions =>
                listenOptions.Protocols = HttpProtocols.Http1));

        // Register fakes BEFORE AddEventStoreServer (TryAdd won't override these)
        _ = builder.Services.AddSingleton<IEventPublisher>(EventPublisher);
        _ = builder.Services.AddSingleton<IDeadLetterPublisher>(DeadLetterPublisher);
        _ = builder.Services.AddSingleton<ICommandStatusStore>(CommandStatusStore);

        // Register DAPR client and EventStore server infrastructure (actors, command routing, REAL domain service invoker)
        builder.Services.AddDaprClient();
        _ = builder.Services.AddEventStoreServer(builder.Configuration);

        // Register real domain processors (TenantAggregate, GlobalAdministratorsAggregate)
        _ = builder.Services.AddEventStore(typeof(TenantAggregate).Assembly);

        // Register domain service request handler for /process endpoint
        _ = builder.Services.AddScoped<DomainServiceRequestHandler>();

        _testHost = builder.Build();

        // Map endpoints
        _ = _testHost.MapActorsHandlers();
        _ = _testHost.MapPost("/process", async (
            DomainServiceRequest request,
            DomainServiceRequestHandler handler,
            ILogger<TenantsDaprTestFixture> logger,
            CancellationToken cancellationToken) => {
                try {
                    DomainServiceWireResult result = await handler.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
                    return Microsoft.AspNetCore.Http.Results.Ok(result);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Domain processing failed for command type {CommandType}", request.Command.CommandType);
                    return Microsoft.AspNetCore.Http.Results.Problem(
                        detail: ex.ToString(),
                        statusCode: 500);
                }
            });
        _ = _testHost.MapGet("/healthz", () => Microsoft.AspNetCore.Http.Results.Ok("healthy"));

        await _testHost.StartAsync().ConfigureAwait(false);

        IServer server = _testHost.Services.GetRequiredService<IServer>();
        ICollection<string>? addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0) {
            throw new InvalidOperationException(
                $"Kestrel did not bind to any addresses. Expected port {_appPort}.");
        }
    }

    private void StartDaprSidecar() {
        string daprdPath = ResolveDaprdPath();

        _daprProcess = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = daprdPath,
                Arguments = string.Join(' ',
                    "--app-id", AppId,
                    "--app-port", _appPort.ToString(),
                    "--app-protocol", "http",
                    "--app-channel-address", "127.0.0.1",
                    "--dapr-http-port", _daprHttpPort.ToString(),
                    "--dapr-grpc-port", _daprGrpcPort.ToString(),
                    "--dapr-internal-grpc-port", _daprInternalGrpcPort.ToString(),
                    "--metrics-port", _daprMetricsPort.ToString(),
                    "--profile-port", _daprProfilePort.ToString(),
                    "--resources-path", $"\"{_componentsDir}\"",
                    "--log-level", "info",
                    "--placement-host-address", $"localhost:{PlacementPort}",
                    "--scheduler-host-address", $"localhost:{SchedulerPort}"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        _daprProcess.OutputDataReceived += (_, e) => {
            if (e.Data is not null) {
                lock (_daprStdout) {
                    _ = _daprStdout.AppendLine(e.Data);
                }
            }
        };

        _daprProcess.ErrorDataReceived += (_, e) => {
            if (e.Data is not null) {
                lock (_daprStderr) {
                    _ = _daprStderr.AppendLine(e.Data);
                }
            }
        };

        _ = _daprProcess.Start();
        _daprProcess.BeginOutputReadLine();
        _daprProcess.BeginErrorReadLine();

        if (_daprProcess.HasExited) {
            throw new InvalidOperationException(
                $"daprd exited immediately with code {_daprProcess.ExitCode}.\nstderr: {GetCapturedStderr()}");
        }
    }

    private static string CreateComponentFiles() {
        string tempDir = Path.Combine(Path.GetTempPath(), $"dapr-tenants-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempDir);

        string stateStoreYaml = $$"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: statestore
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "localhost:{{RedisPort}}"
                - name: redisPassword
                  value: ""
                - name: actorStateStore
                  value: "true"
            scopes:
              - commandapi
            """;

        string pubSubYaml = $$"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: pubsub
            spec:
              type: pubsub.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "localhost:{{RedisPort}}"
                - name: redisPassword
                  value: ""
                - name: enableDeadLetter
                  value: "true"
            scopes:
              - commandapi
            """;

        File.WriteAllText(Path.Combine(tempDir, "statestore.yaml"), stateStoreYaml);
        File.WriteAllText(Path.Combine(tempDir, "pubsub.yaml"), pubSubYaml);

        return tempDir;
    }

    private async Task WaitForDaprHealthAsync() {
        using var httpClient = new HttpClient();
        string healthUrl = $"{DaprHttpEndpoint}/v1.0/healthz/outbound";

        HttpStatusCode lastStatus = default;
        string? lastError = null;

        for (int i = 0; i < HealthTimeoutSeconds; i++) {
            if (_daprProcess?.HasExited == true) {
                throw new InvalidOperationException(
                    $"daprd exited with code {_daprProcess.ExitCode} during health check.\n" +
                    $"stdout:\n{GetCapturedStdout()}\n" +
                    $"stderr:\n{GetCapturedStderr()}");
            }

            try {
                HttpResponseMessage response = await httpClient.GetAsync(healthUrl).ConfigureAwait(false);
                lastStatus = response.StatusCode;
                if (response.IsSuccessStatusCode) {
                    return;
                }
            }
            catch (HttpRequestException ex) {
                lastError = ex.Message;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Dapr sidecar did not become healthy within {HealthTimeoutSeconds} seconds.\n" +
            $"Health endpoint: {healthUrl}\n" +
            $"Last HTTP status: {lastStatus}\n" +
            $"Last connection error: {lastError ?? "(none)"}\n" +
            $"Ports: app={_appPort}, daprHttp={_daprHttpPort}, daprGrpc={_daprGrpcPort}\n" +
            $"--- daprd stdout (last 2000 chars) ---\n{TailString(GetCapturedStdout(), 2000)}\n" +
            $"--- daprd stderr (last 2000 chars) ---\n{TailString(GetCapturedStderr(), 2000)}");
    }

    private async Task VerifyAppListeningAsync() {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        string healthUrl = $"http://127.0.0.1:{_appPort}/healthz";
        string? lastError = null;

        for (int i = 0; i < 30; i++) {
            try {
                _ = await httpClient.GetAsync(healthUrl).ConfigureAwait(false);
                return;
            }
            catch (HttpRequestException ex) {
                lastError = ex.Message;
            }
            catch (TaskCanceledException) {
                lastError = "Request timed out";
            }

            await Task.Delay(200).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Test host HTTP check failed on http://127.0.0.1:{_appPort} after 30 attempts.\n" +
            $"Last HTTP error: {lastError}");
    }

    private static void KillOrphanedDaprdProcesses() {
        if (Environment.GetEnvironmentVariable("DAPR_TEST_PRESERVE_SIDECARS") == "1") {
            return;
        }

        try {
            foreach (Process process in Process.GetProcessesByName("daprd")) {
                try {
                    string? cmdLine = GetProcessCommandLine(process);
                    if (cmdLine is null || !cmdLine.Contains(AppId, StringComparison.OrdinalIgnoreCase)) {
                        process.Dispose();
                        continue;
                    }

                    process.Kill(entireProcessTree: true);
                    _ = process.WaitForExit(5000);
                }
                catch {
                    // Best-effort cleanup
                }
                finally {
                    process.Dispose();
                }
            }
        }
        catch {
            // Best-effort cleanup
        }
    }

    private static string? GetProcessCommandLine(Process process) {
        try {
            if (OperatingSystem.IsWindows()) {
                using var searcher = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = "wmic",
                        Arguments = $"process where processid={process.Id} get CommandLine /format:list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
                };
                _ = searcher.Start();
                string output = searcher.StandardOutput.ReadToEnd();
                _ = searcher.WaitForExit(3000);
                return output;
            }

            string cmdlinePath = $"/proc/{process.Id}/cmdline";
            if (File.Exists(cmdlinePath)) {
                return File.ReadAllText(cmdlinePath).Replace('\0', ' ');
            }
        }
        catch {
            // Best-effort
        }

        return null;
    }

    private static string ResolveDaprdPath() {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string candidate = Path.Combine(home, ".dapr", "bin", "daprd" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));

        if (File.Exists(candidate)) {
            return candidate;
        }

        return OperatingSystem.IsWindows() ? "daprd.exe" : "daprd";
    }

    private static int[] GetAvailablePorts(int count) {
        var listeners = new TcpListener[count];
        int[] ports = new int[count];

        for (int i = 0; i < count; i++) {
            listeners[i] = new TcpListener(IPAddress.Loopback, 0);
            listeners[i].Start();
            ports[i] = ((IPEndPoint)listeners[i].LocalEndpoint).Port;
        }

        for (int i = 0; i < count; i++) {
            listeners[i].Stop();
        }

        return ports;
    }

    private string GetCapturedStdout() {
        lock (_daprStdout) { return _daprStdout.ToString(); }
    }

    private string GetCapturedStderr() {
        lock (_daprStderr) { return _daprStderr.ToString(); }
    }

    private static string TailString(string value, int maxChars) {
        if (string.IsNullOrEmpty(value) || value.Length <= maxChars) {
            return value;
        }

        return "..." + value[^maxChars..];
    }
}
