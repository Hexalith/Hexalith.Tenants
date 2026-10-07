using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = CreateService(scopeFactory, options, lifetime, logger);

        // Act
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => capturedBody is not null);

        // Assert — verify the HTTP request contained the correct command payload
        capturedBody.ShouldNotBeNull();
        using JsonDocument doc = JsonDocument.Parse(capturedBody);
        JsonElement root = doc.RootElement;
        root.GetProperty("tenant").GetString().ShouldBe("system");
        // Bootstrap targets the global-administrators aggregate via its dedicated domain so
        // the resulting projection request arrives at /project with
        // ProjectionRequest.Domain == "global-administrators" and routes to
        // GlobalAdministratorProjectionHandler (story tenant-management-debug-cluster-fix ST3).
        root.GetProperty("domain").GetString().ShouldBe("global-administrators");
        root.GetProperty("aggregateId").GetString().ShouldBe("global-administrators");
        root.GetProperty("commandType").GetString().ShouldBe("BootstrapGlobalAdmin");
        string messageId = root.GetProperty("messageId").GetString().ShouldNotBeNull();
        string correlationId = root.GetProperty("correlationId").GetString().ShouldNotBeNull();
        messageId.Length.ShouldBe(26);
        correlationId.Length.ShouldBe(26);
        Should.NotThrow(() => UniqueIdHelper.ExtractTimestamp(messageId));
        Should.NotThrow(() => UniqueIdHelper.ExtractTimestamp(correlationId));
        logger.Messages.ShouldContain(m => m.Contains("configured global administrator", StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains("admin-user-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_with_configured_userId_waits_until_application_started() {
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, _) => {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        httpCalled.ShouldBeFalse();

        lifetime.StartApplication();
        await WaitUntilAsync(() => httpCalled);
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
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = userId,
        });

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

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
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(
            () => service.StartAsync(CancellationToken.None));
        lifetime.StartApplication();
    }

    [Fact]
    public async Task StartAsync_when_non_accepted_response_logs_unexpected_response() {
        // Arrange — EventStore returns 409 Conflict (e.g., global admin already bootstrapped)
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) {
                Content = new StringContent("{\"detail\":\"Global administrator already bootstrapped\"}"),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = CreateService(scopeFactory, options, lifetime, logger);

        // Act
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Messages.Any(m => m.Contains("409", StringComparison.Ordinal)));

        // Assert — logs the unexpected response with status code
        logger.Messages.ShouldContain(m => m.Contains("409", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_when_already_bootstrapped_rejection_logs_information_without_response_body() {
        const string sensitiveBody = "{\"type\":\"GlobalAdminAlreadyBootstrappedRejection\",\"payload\":{\"token\":\"secret-token\"}}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = CreateService(scopeFactory, options, lifetime, logger);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(e => e.EventId.Id == 2004));

        TestLogEntry entry = logger.Entries.Single(e => e.EventId.Id == 2004);
        entry.LogLevel.ShouldBe(LogLevel.Information);
        entry.Message.ShouldContain("already");
        entry.Message.ShouldNotContain(sensitiveBody);
        entry.Message.ShouldNotContain("secret-token");
        entry.Message.ShouldNotContain("admin-user-1");
    }

    [Fact]
    public async Task StartAsync_when_non_conflict_body_mentions_already_bootstrapped_logs_unexpected_response() {
        const string sensitiveBody = "{\"type\":\"GlobalAdminAlreadyBootstrappedRejection\",\"payload\":{\"token\":\"secret-token\"}}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = CreateService(scopeFactory, options, lifetime, logger);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Messages.Any(m => m.Contains("500", StringComparison.Ordinal)));

        logger.Entries.ShouldNotContain(e => e.EventId.Id == 2004);
        logger.Messages.ShouldContain(m => m.Contains("500", StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains(sensitiveBody, StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains("secret-token", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_when_unexpected_response_logs_status_without_response_body() {
        const string sensitiveBody = "{\"detail\":\"do-not-log-command-payload-or-token\"}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = CreateService(scopeFactory, options, lifetime, logger);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Messages.Any(m => m.Contains("500", StringComparison.Ordinal)));

        logger.Messages.ShouldContain(m => m.Contains("500", StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains(sensitiveBody, StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains("do-not-log-command-payload-or-token", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_when_cancelled_before_application_start_does_not_send_command() {
        // Arrange — handler honours cancellation
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, ct) => {
            httpCalled = true;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

        // Act
        await service.StartAsync(cts.Token);
        lifetime.StartApplication();
        await Task.Delay(50);

        // Assert
        httpCalled.ShouldBeFalse();
    }

    // EventStore Story 5.5: the bootstrap is authorized only by the configured administrator's delegated credential.
    [Fact]
    public async Task Development_symmetric_credential_is_a_short_lived_delegated_token_for_the_configured_administrator() {
        string? authorization = null;
        var handler = new TestHttpMessageHandler((request, _) => {
            authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var lifetime = new TestHostApplicationLifetime();
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }),
            lifetime,
            NullLogger<TenantBootstrapHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => authorization is not null);

        authorization.ShouldStartWith("Bearer ");
        string token = authorization!["Bearer ".Length..];
        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters {
            ValidIssuer = DevelopmentIssuer,
            ValidAudience = DevelopmentAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevelopmentSigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(60),
        });
        validation.IsValid.ShouldBeTrue(validation.Exception?.Message);
        var jwt = (JsonWebToken)validation.SecurityToken;
        jwt.Subject.ShouldBe("admin-user-1");
        jwt.GetClaim("global_admin").Value.ShouldBe("true");
        jwt.GetClaim("eventstore:tenant").Value.ShouldBe("system");
        (jwt.ValidTo - jwt.IssuedAt).ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(TenantBootstrapCredentialProvider.DevelopmentTokenLifetimeSeconds));
    }

    [Theory]
    [InlineData("no-credential")]
    [InlineData("production-signing-key")]
    [InlineData("authority-without-password")]
    public async Task Without_a_delegated_credential_the_command_is_not_sent(string scenario) {
        ArgumentNullException.ThrowIfNull(scenario);
        bool commandSent = false;
        var handler = new TestHttpMessageHandler((request, _) => {
            commandSent = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        Dictionary<string, string?> settings = scenario switch {
            "no-credential" => [],
            "production-signing-key" => DevelopmentContract(),
            _ => new() {
                ["EventStore:Authentication:Authority"] = "https://identity.example.test/realms/hexalith",
                ["EventStore:Authentication:Username"] = "admin",
            },
        };
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }),
            lifetime,
            logger,
            settings,
            scenario == "production-signing-key" ? Environments.Production : Environments.Development);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2006));

        commandSent.ShouldBeFalse(scenario);
        logger.Messages.ShouldNotContain(static message => message.Contains("admin-user-1", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authority_mode_sends_only_the_configured_administrators_own_token(bool subjectMatches) {
        string adminToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Issuer = "https://identity.example.test/realms/hexalith",
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = subjectMatches ? "admin-user-1" : "someone-else" },
        });
        string? tokenRequest = null;
        string? commandAuthorization = null;
        var handler = new TestHttpMessageHandler(async (request, cancellationToken) => {
            if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal)) {
                tokenRequest = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, object> { ["access_token"] = adminToken, ["expires_in"] = 300 })),
                };
            }

            commandAuthorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }),
            lifetime,
            logger,
            new Dictionary<string, string?> {
                ["EventStore:Authentication:Authority"] = "https://identity.example.test/realms/hexalith",
                ["EventStore:Authentication:ClientId"] = "hexalith-eventstore",
                ["EventStore:Authentication:Username"] = "admin",
                ["EventStore:Authentication:Password"] = Guid.NewGuid().ToString("N"),
            });

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => commandAuthorization is not null || logger.Entries.Any(static entry => entry.EventId.Id == 2006));

        tokenRequest.ShouldNotBeNull();
        tokenRequest.ShouldContain("grant_type=password");
        if (subjectMatches) {
            commandAuthorization.ShouldBe("Bearer " + adminToken);
        }
        else {
            commandAuthorization.ShouldBeNull();
            logger.Messages.ShouldContain(static message => message.Contains("authority-subject-mismatch", StringComparison.Ordinal));
        }
    }

    private const string DevelopmentIssuer = "hexalith-dev";
    private const string DevelopmentAudience = "hexalith-eventstore";
    private static readonly string DevelopmentSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static Dictionary<string, string?> DevelopmentContract()
        => new(StringComparer.Ordinal) {
            ["Authentication:JwtBearer:Issuer"] = DevelopmentIssuer,
            ["Authentication:JwtBearer:Audience"] = DevelopmentAudience,
            ["Authentication:JwtBearer:SigningKey"] = DevelopmentSigningKey,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256",
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
        };

    private static TenantBootstrapHostedService CreateService(
        IServiceScopeFactory scopeFactory,
        IOptions<TenantBootstrapOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<TenantBootstrapHostedService> logger,
        Dictionary<string, string?>? settings = null,
        string environmentName = "Development") {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new TenantBootstrapHostedService(
            scopeFactory,
            options,
            new ConfigurationBuilder().AddInMemoryCollection(settings ?? DevelopmentContract()).Build(),
            environment,
            lifetime,
            logger);
    }

    private static async Task WaitUntilAsync(Func<bool> condition) {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition()) {
            await Task.Delay(10, cts.Token);
        }
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

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public CancellationToken ApplicationStarted => _started.Token;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => _stopped.Token;

        public void StopApplication() => _stopping.Cancel();

        public void StartApplication() => _started.Cancel();

        public void Dispose() {
            _started.Dispose();
            _stopping.Dispose();
            _stopped.Dispose();
        }
    }

    private sealed class TestLogger<T> : ILogger<T> {
        private readonly ConcurrentQueue<string> _messages = new();
        private readonly ConcurrentQueue<TestLogEntry> _entries = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();
        public IReadOnlyCollection<TestLogEntry> Entries => _entries.ToArray();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            string message = formatter(state, exception);
            _messages.Enqueue(message);
            _entries.Enqueue(new TestLogEntry(logLevel, eventId, message));
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, EventId EventId, string Message);
}
