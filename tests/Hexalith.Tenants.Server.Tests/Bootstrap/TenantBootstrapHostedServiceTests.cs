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
    public async Task StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator() {
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, _) => {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Tenants:BootstrapGlobalAdminEnabled"] = "false",
            ["Tenants:BootstrapGlobalAdminUserId"] = "admin-user-1",
        }).Build();
        TenantBootstrapOptions boundOptions = configuration.GetSection("Tenants").Get<TenantBootstrapOptions>()!;
        boundOptions.BootstrapGlobalAdminUserId.ShouldBe("admin-user-1");
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(boundOptions),
            lifetime,
            logger);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await Task.Delay(50);

        httpCalled.ShouldBeFalse();
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 2007);
    }

    [Fact]
    public void BootstrapControlDefaultsToEnabled() {
        new TenantBootstrapOptions().BootstrapGlobalAdminEnabled.ShouldBeTrue();
    }

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
    [InlineData("Production", null)]
    [InlineData("Production", "")]
    [InlineData("Production", "   ")]
    [InlineData("Production", "http://identity.example.test/realms/hexalith")]
    [InlineData("Production", "realms/hexalith")]
    [InlineData("Production", "https:///realms/hexalith")]
    [InlineData("Production", "https://identity.example.test:invalid/realms/hexalith")]
    [InlineData("Production", "https://identity.example.test:65536/realms/hexalith")]
    [InlineData("Production", "https://identity.example.test/realms/invalid realm")]
    [InlineData("Production", "https://identity.example.test/realms/%invalid")]
    [InlineData("Production", "https://identity.example.test/realms\\hexalith")]
    [InlineData("Production", "ftp://identity.example.test/realms/hexalith")]
    [InlineData("Production", "https://diagnostic-username:diagnostic-password@identity.example.test/realms/hexalith")]
    [InlineData("Production", "https://identity.example.test/realms/hexalith?token=diagnostic-token")]
    [InlineData("Production", "https://identity.example.test/realms/hexalith#diagnostic-token")]
    [InlineData("Staging", "http://identity.example.test/realms/hexalith")]
    [InlineData("Staging", "not-an-authority")]
    [InlineData("ReleaseCandidate", "http://identity.example.test/realms/hexalith")]
    [InlineData("ReleaseCandidate", "not-an-authority")]
    [InlineData("Development", "realms/hexalith")]
    [InlineData("Development", "ftp://localhost/realms/hexalith")]
    [InlineData("Development", "http://localhost:invalid/realms/hexalith")]
    [InlineData("Development", "http://localhost/realms/invalid realm")]
    [InlineData("Development", "http://localhost/realms/%invalid")]
    [InlineData("Development", "http://localhost/realms\\hexalith")]
    [InlineData("Development", "http://diagnostic-username:diagnostic-password@localhost/realms/hexalith")]
    [InlineData("Development", "http://localhost/realms/hexalith?token=diagnostic-token")]
    [InlineData("Development", "http://localhost/realms/hexalith#diagnostic-token")]
    public async Task InvalidAuthorityConfigurationSendsNoHttpRequests(string environmentName, string? authority) {
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) => {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        Dictionary<string, string?> settings = DevelopmentContract();
        settings["EventStore:Authentication:Authority"] = authority;
        settings["EventStore:Authentication:Username"] = "diagnostic-username";
        settings["EventStore:Authentication:Password"] = "diagnostic-password";
        settings["EventStore:Authentication:ClientId"] = "diagnostic-client";
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var provider = new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            environment,
            TimeProvider.System,
            logger);

        string? token = await provider.AcquireAsync("diagnostic-administrator", httpClient, CancellationToken.None);

        token.ShouldBeNull();
        requestCount.ShouldBe(0);
        logger.Entries.ShouldContain(static entry => entry.EventId.Id == 2006);
        AssertSupportSafeDiagnostics(logger, "diagnostic-username", "diagnostic-password", "diagnostic-client", "diagnostic-administrator", "diagnostic-token", DevelopmentSigningKey);
        if (!string.IsNullOrWhiteSpace(authority)) {
            AssertSupportSafeDiagnostics(logger, authority);
        }
    }

    [Theory]
    [InlineData("Production", "https://identity.example.test/realms/hexalith", true)]
    [InlineData("Production", "https://identity.example.test/realms/hexalith", false)]
    [InlineData("Production", "https://identity.example.test/realms/encoded%20realm", true)]
    [InlineData("Staging", "https://identity.example.test/realms/hexalith/", true)]
    [InlineData("ReleaseCandidate", "https://identity.example.test/realms/hexalith", true)]
    [InlineData("Development", "http://localhost:18080/realms/hexalith", true)]
    [InlineData("Development", "http://localhost:18080/realms/hexalith", false)]
    [InlineData("Development", "https://identity.example.test/realms/hexalith", true)]
    public async Task ValidAuthorityPreservesRealmPathAndRequiresAdministratorSubject(string environmentName, string authority, bool subjectMatches) {
        ArgumentNullException.ThrowIfNull(authority);
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        string delegatedToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) {
                ["sub"] = subjectMatches ? administrator : "diagnostic-other-subject",
            },
        });
        int requestCount = 0;
        Uri? capturedUri = null;
        string? capturedForm = null;
        using var handler = new TestHttpMessageHandler(async (request, cancellationToken) => {
            requestCount++;
            request.Method.ShouldBe(HttpMethod.Post);
            capturedUri = request.RequestUri;
            capturedForm = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(JsonSerializer.Serialize(new { access_token = delegatedToken })),
            };
        });
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var provider = new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["EventStore:Authentication:Authority"] = authority,
                ["EventStore:Authentication:Username"] = username,
                ["EventStore:Authentication:Password"] = password,
            }).Build(),
            environment,
            TimeProvider.System,
            logger);

        string? acquiredToken = await provider.AcquireAsync(administrator, httpClient, CancellationToken.None);

        requestCount.ShouldBe(1);
        capturedUri.ShouldBe(new Uri($"{authority.TrimEnd('/')}/protocol/openid-connect/token"));
        capturedForm.ShouldNotBeNull();
        capturedForm.ShouldContain("grant_type=password");
        capturedForm.ShouldContain($"username={username}");
        capturedForm.ShouldContain($"password={password}");
        acquiredToken.ShouldBe(subjectMatches ? delegatedToken : null);
        if (!subjectMatches) {
            logger.Messages.ShouldContain(static message => message.Contains("authority-subject-mismatch", StringComparison.Ordinal));
        }

        AssertSupportSafeDiagnostics(logger, administrator, username, password, delegatedToken, authority, "diagnostic-other-subject");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.OK)]
    public async Task AuthorityResponseDiagnosticsExcludeCredentialsAndTokens(HttpStatusCode statusCode) {
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        const string malformedToken = "diagnostic-token";
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) => {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode) {
                Content = new StringContent(JsonSerializer.Serialize(new {
                    access_token = malformedToken,
                    username,
                    password,
                })),
            });
        });
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var provider = new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["EventStore:Authentication:Authority"] = "https://identity.example.test/realms/hexalith",
                ["EventStore:Authentication:Username"] = username,
                ["EventStore:Authentication:Password"] = password,
            }).Build(),
            environment,
            TimeProvider.System,
            logger);

        string? token = await provider.AcquireAsync(administrator, httpClient, CancellationToken.None);

        token.ShouldBeNull();
        requestCount.ShouldBe(1);
        logger.Entries.ShouldContain(static entry => (entry.EventId.Id == 2005 || entry.EventId.Id == 2006));
        AssertSupportSafeDiagnostics(logger, administrator, username, password, malformedToken);
    }

    [Theory]
    [InlineData("authority-request", "credential-acquisition", "HttpRequestException", 1)]
    [InlineData("token-json", "credential-acquisition", "JsonReaderException", 1)]
    [InlineData("command-dispatch", "command-dispatch", "HttpRequestException", 2)]
    public async Task BootstrapFailureDiagnosticsExcludeExceptionCredentialsAndTokens(string scenario, string phase, string exceptionType, int expectedRequests) {
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        const string authority = "https://identity.example.test/realms/hexalith";
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = administrator },
        });
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) => {
            requestCount++;
            if (scenario == "token-json")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"sensitive\":\"{username} {password} {token}\",\"invalid\": }}"),
                });
            }

            if (scenario == "command-dispatch" && requestCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { access_token = token })),
                });
            }

            throw new HttpRequestException($"{authority} {username} {password}", new InvalidOperationException(token));
        });
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = administrator }),
            lifetime,
            logger,
            new Dictionary<string, string?> {
                ["EventStore:Authentication:Authority"] = authority,
                ["EventStore:Authentication:Username"] = username,
                ["EventStore:Authentication:Password"] = password,
            },
            Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2002));

        requestCount.ShouldBe(expectedRequests);
        TestLogEntry failure = logger.Entries.Single(static entry => entry.EventId.Id == 2002);
        failure.Message.ShouldContain("bootstrap-operation-failed");
        failure.Message.ShouldContain(phase);
        failure.Message.ShouldContain(exceptionType);
        failure.ExceptionText.ShouldBeNull();
        AssertSupportSafeDiagnostics(logger, administrator, username, password, token, authority);
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

    private static void AssertSupportSafeDiagnostics(TestLogger<TenantBootstrapHostedService> logger, params string[] sensitiveValues) {
        foreach (TestLogEntry entry in logger.Entries) {
            string diagnostics = $"{entry.Message}\n{entry.DiagnosticState}\n{entry.ExceptionText}";
            foreach (string sensitiveValue in sensitiveValues) {
                diagnostics.ShouldNotContain(sensitiveValue);
            }
        }
    }

    [Fact]
    public async Task HttpClientFactoryDiagnosticsExcludeCredentialFormAndBearerToken()
    {
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = administrator },
        });
        int requestCount = 0;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { access_token = token })),
                });
            }

            request.Headers.Authorization!.Parameter.ShouldBe(token);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var logger = new TestLogger<TenantBootstrapHostedService>();
        ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        var services = new ServiceCollection();
        services.AddDaprClient();
        services.AddLogging();
        services.AddSingleton(loggerFactory);
        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));
        await using ServiceProvider provider = services.BuildServiceProvider();
        using var lifetime = new TestHostApplicationLifetime();
        TenantBootstrapHostedService service = CreateService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = administrator }),
            lifetime,
            logger,
            new Dictionary<string, string?>
            {
                ["EventStore:Authentication:Authority"] = "https://identity.example.test/realms/hexalith",
                ["EventStore:Authentication:Username"] = username,
                ["EventStore:Authentication:Password"] = password,
            },
            Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2001));

        requestCount.ShouldBe(2);
        logger.Messages.ShouldContain(static message => message.Contains("Sending HTTP request", StringComparison.Ordinal));
        logger.Messages.ShouldContain(static message => message.Contains("Authorization: *", StringComparison.Ordinal));
        AssertSupportSafeDiagnostics(logger, administrator, username, password, token);
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
            string diagnosticState = state is IEnumerable<KeyValuePair<string, object?>> fields
                ? string.Join("\n", fields.Select(static field => $"{field.Key}={field.Value}"))
                : state?.ToString() ?? string.Empty;
            _entries.Enqueue(new TestLogEntry(logLevel, eventId, message, diagnosticState, exception?.ToString()));
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, EventId EventId, string Message, string DiagnosticState, string? ExceptionText);
}
