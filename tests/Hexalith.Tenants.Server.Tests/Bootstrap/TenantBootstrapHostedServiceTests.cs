using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

public class TenantBootstrapHostedServiceTests {
    [Fact]
    public async Task StartAsync_with_configured_userId_sends_BootstrapGlobalAdmin_command() {
        // Arrange
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(async (request, _) => {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = new TenantBootstrapHostedService(scopeFactory, options, logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — verify the HTTP request contained the correct command payload
        capturedBody.ShouldNotBeNull();
        using JsonDocument doc = JsonDocument.Parse(capturedBody);
        JsonElement root = doc.RootElement;
        root.GetProperty("tenant").GetString().ShouldBe("system");
        root.GetProperty("domain").GetString().ShouldBe("tenants");
        root.GetProperty("aggregateId").GetString().ShouldBe("global-administrators");
        root.GetProperty("commandType").GetString().ShouldBe("BootstrapGlobalAdmin");
        logger.Messages.ShouldContain(m => m.Contains("admin-user-1", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAsync_with_empty_userId_skips_bootstrap(string? userId) {
        // Arrange
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, _) => {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = userId,
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — no HTTP request sent
        httpCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_handles_infrastructure_exception_without_crashing() {
        // Arrange — handler simulates a network-level failure
        var handler = new TestHttpMessageHandler((_, _)
            => throw new HttpRequestException("DAPR sidecar not ready"));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(
            () => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_when_non_accepted_response_logs_unexpected_response() {
        // Arrange — EventStore returns 409 Conflict (e.g., global admin already bootstrapped)
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) {
                Content = new StringContent("{\"detail\":\"Global administrator already bootstrapped\"}"),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = new TenantBootstrapHostedService(scopeFactory, options, logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — logs the unexpected response with status code
        logger.Messages.ShouldContain(m => m.Contains("409", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_when_cancelled_propagates_OperationCanceledException() {
        // Arrange — handler honours cancellation
        var handler = new TestHttpMessageHandler((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should propagate directly, not be swallowed
        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => service.StartAsync(cts.Token));
    }

    private static IServiceScopeFactory CreateScopeFactory(TestHttpMessageHandler handler) {
        ServiceCollection services = new();
        services.AddDaprClient();

        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        _ = services.AddSingleton(httpClientFactory);

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }

    private sealed class TestLogger<T> : ILogger<T> {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _messages.Enqueue(formatter(state, exception));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
