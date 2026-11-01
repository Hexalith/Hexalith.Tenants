using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.ErrorHandling;
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

public class TenantBootstrapHostedServiceTests
{
    [Fact]
    public async Task CredentialsAndCommandsUseTheirSeparatelyOwnedClients()
    {
        using var credentialHandler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = CreateAuthorityToken() })),
        }));
        using var commandHandler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.ShouldEndWith("/api/v1/commands");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        using var credentialClient = new HttpClient(credentialHandler);
        using var readinessClient = new HttpClient();
        using var commandClient = new HttpClient(commandHandler);
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(string.Empty).Returns(readinessClient);
        factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName).Returns(commandClient);
        factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CredentialClientName).Returns(credentialClient);
        var services = new ServiceCollection();
        services.AddDaprClient();
        services.AddSingleton(factory);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime, logger, AuthoritySettings(), Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2001));

        factory.Received(1).CreateClient(string.Empty);
        factory.Received(1).CreateClient(TenantBootstrapServiceCollectionExtensions.CredentialClientName);
        factory.Received(1).CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName);
    }

    [Fact]
    public async Task StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator()
    {
        bool httpCalled = false;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        using var started = new CancellationTokenSource();
        using var stopping = new CancellationTokenSource();
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        lifetime.ClearReceivedCalls();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenants:BootstrapGlobalAdminEnabled"] = "false",
            ["Tenants:BootstrapGlobalAdminUserId"] = "admin-user-1",
        }).Build();
        TenantBootstrapOptions boundOptions = configuration.GetSection("Tenants").Get<TenantBootstrapOptions>()!;
        boundOptions.BootstrapGlobalAdminEnabled.ShouldBeFalse();
        boundOptions.BootstrapGlobalAdminUserId.ShouldBe("admin-user-1");
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(boundOptions),
            lifetime,
            logger);

        await service.StartAsync(CancellationToken.None);

        // No started token was obtained, so no deferred startup callback can be registered.
        _ = lifetime.DidNotReceive().ApplicationStarted;
        _ = lifetime.DidNotReceive().ApplicationStopping;
        started.Cancel();
        _ = lifetime.DidNotReceive().ApplicationStarted;
        _ = lifetime.DidNotReceive().ApplicationStopping;
        httpCalled.ShouldBeFalse();
        boundOptions.BootstrapGlobalAdminUserId.ShouldBe("admin-user-1");
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 2007);
    }

    [Fact]
    public void BootstrapControlDefaultsToEnabled()
    {
        new TenantBootstrapOptions().BootstrapGlobalAdminEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_with_configured_userId_sends_BootstrapGlobalAdmin_command()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(async (request, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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
    public async Task StartAsyncWithEnabledBootstrapReadsLifetimeAndSendsAfterStartup()
    {
        using var started = new CancellationTokenSource();
        var commandSent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopeDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref requestCount);
            commandSent.TrySetResult(started.IsCancellationRequested);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        await using AsyncServiceScope realScope = CreateScopeFactory(handler).CreateAsyncScope();
        IServiceScope observedScope = Substitute.For<IServiceScope, IAsyncDisposable>();
        observedScope.ServiceProvider.Returns(realScope.ServiceProvider);
        ((IAsyncDisposable)observedScope).DisposeAsync().Returns(_ =>
        {
            scopeDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });
        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(observedScope);
        using var stopping = new CancellationTokenSource();
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        lifetime.ClearReceivedCalls();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(scopeFactory, options, lifetime, logger);

        await service.StartAsync(CancellationToken.None);

        _ = lifetime.Received(1).ApplicationStarted;
        _ = lifetime.Received(1).ApplicationStopping;

        started.Cancel();
        (await commandSent.Task.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeTrue();
        await scopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Volatile.Read(ref requestCount).ShouldBe(1);
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 2001);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAsync_with_empty_userId_skips_bootstrap(string? userId)
    {
        // Arrange
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
            BootstrapGlobalAdminUserId = userId,
        });

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — no HTTP request sent
        httpCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_handles_infrastructure_exception_without_crashing()
    {
        // Arrange — handler simulates a network-level failure
        var handler = new TestHttpMessageHandler((_, _)
            => throw new HttpRequestException("DAPR sidecar not ready"));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = CreateService(scopeFactory, options, lifetime, NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(
            () => service.StartAsync(CancellationToken.None));
        lifetime.StartApplication();
    }

    [Fact]
    public async Task StartAsync_when_non_accepted_response_logs_unexpected_response()
    {
        // Arrange — EventStore returns 409 Conflict (e.g., global admin already bootstrapped)
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("{\"detail\":\"Global administrator already bootstrapped\"}"),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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
    public async Task StartAsync_when_already_bootstrapped_rejection_logs_information_without_response_body()
    {
        const string sensitiveBody = "{\"type\":\"GlobalAdminAlreadyBootstrappedRejection\",\"payload\":{\"token\":\"secret-token\"}}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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
    public async Task StartAsync_when_non_conflict_body_mentions_already_bootstrapped_logs_unexpected_response()
    {
        const string sensitiveBody = "{\"type\":\"GlobalAdminAlreadyBootstrappedRejection\",\"payload\":{\"token\":\"secret-token\"}}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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
    public async Task StartAsync_when_unexpected_response_logs_status_without_response_body()
    {
        const string sensitiveBody = "{\"detail\":\"do-not-log-command-payload-or-token\"}";
        var handler = new TestHttpMessageHandler((_, _)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(sensitiveBody),
            }));

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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

    /// <summary>Logs only recognized SDK classifications even when every other response field carries protected content.</summary>
    [Theory]
    [InlineData("errorCode", "ERR_DIRECT_INVOKE", "dapr", "ERR_DIRECT_INVOKE")]
    [InlineData("errorCode", "ERR_ACTOR_INVOKE_METHOD", "dapr", "ERR_ACTOR_INVOKE_METHOD")]
    [InlineData("errorCode", "ERR_STATE_SAVE", "dapr", "ERR_STATE_SAVE")]
    [InlineData("errorCode", "ERR_SECRET_TOKEN", "dapr", "unrecognized")]
    [InlineData("errorCode", "ERR_DIRECT_INVOKE\nprotected-response-marker", "dapr", "unrecognized")]
    [InlineData("type", "https://hexalith.io/problems/validation-error", "problem-validation-error", "unrecognized")]
    [InlineData("type", "https://hexalith.io/problems/bad-request", "problem-bad-request", "unrecognized")]
    [InlineData("type", "https://tools.ietf.org/html/rfc9110#section-15.5.1", "problem-bad-request", "unrecognized")]
    [InlineData("type", "https://private.example.test?token=protected-response-marker", "problem-other", "unrecognized")]
    [InlineData("reasonCode", "idempotency_admission_rejected", "json-object", "idempotency_admission_rejected")]
    [InlineData("reasonCode", "protected-response-marker", "json-object", "unrecognized")]
    public async Task UnexpectedResponseLogsOnlyClosedSdkClassification(string field, string value, string expectedKind, string expectedCode)
    {
        ArgumentNullException.ThrowIfNull(field);
        const string marker = "protected-response-marker";
        const string privateUrl = "https://private.example.test?token=protected-response-marker";
        var body = new Dictionary<string, object?>
        {
            ["detail"] = marker,
            ["title"] = marker,
            ["message"] = privateUrl,
            ["instance"] = privateUrl,
            ["payload"] = marker,
            ["errors"] = new Dictionary<string, string> { [marker] = privateUrl },
            ["reasonCode"] = marker,
            [field] = value,
        };
        string json = JsonSerializer.Serialize(body);
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json),
        }));
        var logger = new TestLogger<TenantBootstrapHostedService>();
        using var lifetime = new TestHostApplicationLifetime();
        var service = CreateService(CreateScopeFactory(handler), Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }), lifetime, logger);
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.EventId.Id == 2003));
        TestLogEntry entry = logger.Entries.Single(entry => entry.EventId.Id == 2003);
        entry.DiagnosticState.ShouldContain("ResponseKind=" + expectedKind);
        entry.DiagnosticState.ShouldContain("ResponseCode=" + expectedCode);
        entry.DiagnosticState.ShouldContain("StatusCode=400");
        entry.ExceptionText.ShouldBeNull();
        AssertSupportSafeDiagnostics(logger, marker, privateUrl, json);
        if (expectedCode != value) AssertSupportSafeDiagnostics(logger, value);
        logger.Entries.ShouldNotContain(entry => entry.EventId.Id == 2004);
    }

    /// <summary>Malformed, non-object and oversized bodies remain support-safe and never prevent failure reporting.</summary>
    [Theory]
    [InlineData("malformed", "non-json", "none")]
    [InlineData("array", "json-other", "none")]
    [InlineData("nonstring-code", "dapr", "unrecognized")]
    [InlineData("oversized", "unavailable", "none")]
    [InlineData("oversized-unicode", "unavailable", "none")]
    [InlineData("oversized-unknown", "unavailable", "none")]
    [InlineData("deep", "non-json", "none")]
    [InlineData("empty", "unavailable", "none")]
    [InlineData("whitespace", "unavailable", "none")]
    public async Task UnexpectedResponseClassificationKeepsMalformedAndOversizedBodiesPrivate(string scenario, string expectedKind, string expectedCode)
    {
        const string marker = "protected-response-marker";
        string body = scenario switch
        {
            "empty" => string.Empty,
            "whitespace" => " \r\n\t ",
            "array" => JsonSerializer.Serialize(new[] { marker }),
            "nonstring-code" => JsonSerializer.Serialize(new { errorCode = new[] { marker }, detail = marker }),
            "oversized" or "oversized-unknown" => JsonSerializer.Serialize(new { errorCode = "ERR_DIRECT_INVOKE", detail = marker + new string('x', 8192) }),
            "oversized-unicode" => JsonSerializer.Serialize(new { errorCode = "ERR_DIRECT_INVOKE", detail = marker + new string('é', 4096) }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }),
            "deep" => "{\"detail\":" + new string('[', 20) + JsonSerializer.Serialize(marker) + new string(']', 20) + "}",
            _ => marker,
        };
        using HttpContent responseContent = scenario == "oversized-unknown"
            ? new StreamContent(new BootstrapTokenResponseStream(Encoding.UTF8.GetBytes(body), readChunkSize: 64))
            : new StringContent(body);
        if (scenario == "oversized-unicode")
        {
            body.Length.ShouldBeLessThan(8192);
            Encoding.UTF8.GetByteCount(body).ShouldBeGreaterThan(8192);
            // A prebuffered response can carry an incorrect declared length. The actual
            // UTF-8 size must still be checked before JSON diagnostic classification.
            responseContent.Headers.ContentLength = 1;
        }
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = responseContent,
        }));
        var logger = new TestLogger<TenantBootstrapHostedService>();
        using var lifetime = new TestHostApplicationLifetime();
        var service = CreateService(CreateScopeFactory(handler), Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }), lifetime, logger);
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.EventId.Id == 2003));
        TestLogEntry entry = logger.Entries.Single(entry => entry.EventId.Id == 2003);
        entry.DiagnosticState.Split('\n').ShouldContain("ResponseKind=" + expectedKind);
        entry.DiagnosticState.Split('\n').ShouldContain("ResponseCode=" + expectedCode);
        entry.DiagnosticState.Split('\n').ShouldContain("StatusCode=400");
        entry.ExceptionText.ShouldBeNull();
        AssertSupportSafeDiagnostics(logger, marker);
        if (!string.IsNullOrWhiteSpace(body)) AssertSupportSafeDiagnostics(logger, body);
    }

    /// <summary>A parsed but malformed JSON scalar must retain the actual failure status without leaking escaped content.</summary>
    [Theory]
    [InlineData("errorCode")]
    [InlineData("type")]
    [InlineData("reasonCode")]
    [InlineData("code")]
    public async Task UnpairedSurrogateResponseRetainsUnexpectedStatusAndClosedFallback(string field)
    {
        string body = "{\"" + field + "\":\"\\uD800\",\"detail\":\"protected-response-marker\"}";
        await AssertUnexpectedBootstrapResponseAsync(body, HttpStatusCode.BadRequest, "non-json", "none", false, "\\uD800");
    }

    /// <summary>Matches actual SDK problem shapes, canonical codes and explicit reason-code precedence.</summary>
    [Theory]
    [InlineData(null, null, "idempotency_admission_rejected", 400, "json-object", "idempotency_admission_rejected")]
    [InlineData(null, "principal_not_member", "idempotency_admission_rejected", 403, "json-object", "principal_not_member")]
    [InlineData(null, "protected-response-marker", "idempotency_admission_rejected", 400, "json-object", "unrecognized")]
    [InlineData("forbidden", "principal_not_member", null, 403, "problem-forbidden", "principal_not_member")]
    [InlineData("forbidden", "tenant_mismatch", null, 403, "problem-forbidden", "tenant_mismatch")]
    [InlineData("forbidden", "operation-not-granted", null, 403, "problem-forbidden", "operation-not-granted")]
    [InlineData("authentication-required", "authentication_required", null, 401, "problem-authentication-required", "authentication_required")]
    [InlineData("token-expired", null, null, 401, "problem-token-expired", "none")]
    [InlineData("idempotency-admission-failure", "idempotency_admission_unavailable", "idempotency_admission_unavailable", 503, "problem-idempotency-admission", "idempotency_admission_unavailable")]
    [InlineData("idempotency-conflict", "idempotency_conflict", null, 409, "problem-idempotency-conflict", "idempotency_conflict")]
    [InlineData("idempotency-key-expired", "idempotency_key_expired", null, 409, "problem-idempotency-key-expired", "idempotency_key_expired")]
    [InlineData("unsupported-api-version", null, null, 400, "problem-unsupported-api-version", "none")]
    [InlineData("not-found", null, null, 404, "problem-not-found", "none")]
    [InlineData("not-implemented", null, null, 501, "problem-not-implemented", "none")]
    [InlineData("command-correlation-ambiguous", null, null, 409, "problem-command-correlation-ambiguous", "none")]
    [InlineData("rate-limit-exceeded", null, null, 429, "problem-rate-limit", "none")]
    [InlineData("backpressure-exceeded", null, null, 429, "problem-backpressure", "none")]
    [InlineData("command-status-not-found", null, null, 404, "problem-command-status-not-found", "none")]
    public async Task SdkProblemResponsesRetainStatusAndOnlyClosedClassifications(string? problem, string? reason, string? canonicalCode,
        int status, string expectedKind, string expectedCode)
    {
        const string privateUrl = "https://private.example.test?token=protected-response-marker";
        var body = new Dictionary<string, object?> { ["status"] = status, ["detail"] = "protected-response-marker", ["instance"] = privateUrl };
        string? type = problem is null ? null : "https://hexalith.io/problems/" + problem;
        if (type is not null) body["type"] = type;
        if (reason is not null) body["reasonCode"] = reason;
        if (canonicalCode is not null) body["code"] = canonicalCode;
        await AssertUnexpectedBootstrapResponseAsync(JsonSerializer.Serialize(body), (HttpStatusCode)status, expectedKind, expectedCode, false,
            new[] { type, reason == expectedCode ? null : reason, canonicalCode == expectedCode ? null : canonicalCode, privateUrl }
                .Where(value => value is not null).Select(value => value!).ToArray());
    }

    /// <summary>Classifies the SDK's actual reason-suffixed domain problem URI without copying its suffix or detail.</summary>
    [Fact]
    public async Task ActualSdkDomainRejectionUriIsClassifiedWithoutLoggingItsReasonOrType()
    {
        DomainRejectionProblem problem = DomainRejectionProblemCatalog.FromRejectionType("GlobalAdministratorAlreadyExistsRejection");
        string body = JsonSerializer.Serialize(new { type = problem.TypeUri, reasonCode = problem.ReasonCode, status = problem.StatusCode, detail = "protected-response-marker" });
        await AssertUnexpectedBootstrapResponseAsync(body, (HttpStatusCode)problem.StatusCode, "problem-domain-rejection", "unrecognized", false,
            problem.TypeUri, problem.ReasonCode);
    }

    /// <summary>Accepts exactly 8KiB of UTF-8 diagnostic input and rejects the next byte, independent of character count or declared length.</summary>
    [Theory]
    [InlineData(8192, false, false)]
    [InlineData(8193, false, false)]
    [InlineData(8192, true, false)]
    [InlineData(8193, true, false)]
    [InlineData(8192, false, true)]
    [InlineData(8193, false, true)]
    [InlineData(8192, true, true)]
    [InlineData(8193, true, true)]
    public async Task DiagnosticProbeHasAnExactUtf8ByteBoundary(int bytes, bool multibyte, bool understateLength)
    {
        string body = SizedBootstrapResponse(bytes, multibyte);
        await AssertUnexpectedBootstrapResponseAsync(body, HttpStatusCode.BadRequest, bytes == 8192 ? "dapr" : "unavailable",
            bytes == 8192 ? "ERR_DIRECT_INVOKE" : "none", understateLength);
    }

    /// <summary>Retains ordinary bounded 409 marker handling but never scans an oversized marker body.</summary>
    [Theory]
    [InlineData(8192, false)]
    [InlineData(8193, false)]
    [InlineData(8192, true)]
    [InlineData(8193, true)]
    public async Task AlreadyBootstrappedMarkerMustRemainInsideTheUtf8ProbeLimit(int bytes, bool multibyte)
    {
        string body = SizedBootstrapResponse(bytes, multibyte, alreadyBootstrapped: true);
        using var content = new StringContent(body);
        content.Headers.ContentLength = 1;
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) { Content = content }));
        var logger = new TestLogger<TenantBootstrapHostedService>();
        using var lifetime = new TestHostApplicationLifetime();
        var service = CreateService(CreateScopeFactory(handler), Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }), lifetime, logger);
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.EventId.Id is 2003 or 2004));
        if (bytes == 8192)
        {
            logger.Entries.ShouldContain(entry => entry.EventId.Id == 2004);
            logger.Entries.ShouldNotContain(entry => entry.EventId.Id == 2003);
        }
        else
        {
            TestLogEntry entry = logger.Entries.Single(entry => entry.EventId.Id == 2003);
            entry.DiagnosticState.Split('\n').ShouldContain("StatusCode=409");
            entry.DiagnosticState.Split('\n').ShouldContain("ResponseKind=unavailable");
            entry.DiagnosticState.Split('\n').ShouldContain("ResponseCode=none");
            logger.Entries.ShouldNotContain(entry => entry.EventId.Id == 2004);
        }
        logger.Entries.ShouldNotContain(entry => entry.EventId.Id == 2002);
        AssertSupportSafeDiagnostics(logger, "protected-response-marker", body);
    }

    /// <summary>Classifies SDK validation keys into fixed labels while excluding unknown keys and every message/value.</summary>
    [Theory]
    [InlineData("messageId", "message-id")]
    [InlineData("tenant", "tenant")]
    [InlineData("domain", "domain")]
    [InlineData("aggregateId", "aggregate-id")]
    [InlineData("commandType", "command-type")]
    [InlineData("payload", "payload")]
    [InlineData("payload.UserId", "payload-user-id")]
    [InlineData("correlationId", "correlation-id")]
    [InlineData("extensions", "extensions")]
    [InlineData("idempotencyKey", "idempotency-key")]
    [InlineData("principalId", "principal-id")]
    [InlineData("userId", "user-id")]
    [InlineData("command", "command")]
    [InlineData("$", "request")]
    [InlineData("", "request")]
    [InlineData("$.command.Payload", "payload")]
    [InlineData("request.Command.MessageId", "message-id")]
    [InlineData("protected-response-marker", "unrecognized")]
    [InlineData("payload.protected-response-marker", "unrecognized")]
    [InlineData("extensions\nprotected-response-marker", "unrecognized")]
    public async Task ValidationErrorsExposeOnlyClosedKnownFieldLabels(string field, string expectedField)
    {
        ArgumentNullException.ThrowIfNull(field);
        var problem = ValidationProblemDetailsFactory.Create("protected-response-marker",
            new Dictionary<string, string> { [field] = "https://private.example.test?token=protected-response-marker" },
            "fixture-correlation", "private-tenant-marker");
        string body = JsonSerializer.Serialize(problem);
        TestLogEntry entry = await AssertUnexpectedBootstrapResponseAsync(body, HttpStatusCode.BadRequest, "problem-validation-error", "none", false,
            "private-tenant-marker", "fixture-correlation", "https://private.example.test?token=protected-response-marker");
        entry.DiagnosticState.Split('\n').ShouldContain("ValidationField=" + expectedField);
        if (expectedField == "unrecognized") entry.DiagnosticState.ShouldNotContain(field);
    }

    /// <summary>Non-validation responses never cause the validation dictionary to be interpreted.</summary>
    [Fact]
    public async Task OtherProblemTypesDoNotExposeValidationFields()
    {
        string body = JsonSerializer.Serialize(new { type = ProblemTypeUris.Forbidden, errors = new { payload = "protected-response-marker" } });
        TestLogEntry entry = await AssertUnexpectedBootstrapResponseAsync(body, HttpStatusCode.Forbidden, "problem-forbidden", "none", false, ProblemTypeUris.Forbidden);
        entry.DiagnosticState.Split('\n').ShouldContain("ValidationField=none");
    }

    /// <summary>Malformed and combined validation dictionaries retain the status and emit only closed aggregate labels.</summary>
    [Theory]
    [InlineData("missing", "none")]
    [InlineData("empty", "none")]
    [InlineData("array", "unrecognized")]
    [InlineData("scalar", "unrecognized")]
    [InlineData("null", "unrecognized")]
    [InlineData("different", "multiple")]
    [InlineData("mixed", "multiple")]
    [InlineData("same", "payload")]
    [InlineData("surrogate-key", "unrecognized")]
    public async Task ValidationFieldClassificationKeepsMalformedAndMultipleKeysPrivate(string scenario, string expectedField)
    {
        const string marker = "protected-response-marker";
        string errors = scenario switch
        {
            "empty" => "{}",
            "array" => "[\"protected-response-marker\"]",
            "scalar" => "\"protected-response-marker\"",
            "null" => "null",
            "different" => JsonSerializer.Serialize(new { payload = marker, extensions = marker }),
            "mixed" => JsonSerializer.Serialize(new Dictionary<string, string> { ["payload"] = marker, [marker] = marker }),
            "same" => JsonSerializer.Serialize(new Dictionary<string, string> { ["payload"] = marker, ["command.Payload"] = marker }),
            "surrogate-key" => "{\"\\uD800\":\"protected-response-marker\"}",
            _ => "{}",
        };
        string body = "{\"type\":\"https://hexalith.io/problems/validation-error\""
            + (scenario == "missing" ? string.Empty : ",\"errors\":" + errors) + ",\"detail\":\"protected-response-marker\"}";
        TestLogEntry entry = await AssertUnexpectedBootstrapResponseAsync(body, HttpStatusCode.BadRequest, "problem-validation-error", "none", false, "\\uD800");
        entry.DiagnosticState.Split('\n').ShouldContain("ValidationField=" + expectedField);
    }

    private static string SizedBootstrapResponse(int bytes, bool multibyte, bool alreadyBootstrapped = false)
    {
        string prefix = alreadyBootstrapped
            ? "{\"rejectionType\":\"GlobalAdminAlreadyBootstrappedRejection\",\"detail\":\"protected-response-marker"
            : "{\"errorCode\":\"ERR_DIRECT_INVOKE\",\"detail\":\"protected-response-marker";
        const string suffix = "\"}";
        int padding = bytes - Encoding.UTF8.GetByteCount(prefix + suffix);
        string body = prefix + (multibyte ? new string('é', padding / 2) + new string('x', padding % 2) : new string('x', padding)) + suffix;
        Encoding.UTF8.GetByteCount(body).ShouldBe(bytes);
        return body;
    }

    private static async Task<TestLogEntry> AssertUnexpectedBootstrapResponseAsync(string body, HttpStatusCode status, string kind, string code,
        bool understateLength = false, params string[] protectedValues)
    {
        using var content = new StringContent(body);
        if (understateLength) content.Headers.ContentLength = 1;
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = content }));
        var logger = new TestLogger<TenantBootstrapHostedService>();
        using var lifetime = new TestHostApplicationLifetime();
        var service = CreateService(CreateScopeFactory(handler), Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = "admin-user-1" }), lifetime, logger);
        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.EventId.Id is 2002 or 2003));
        TestLogEntry entry = logger.Entries.Single(entry => entry.EventId.Id == 2003);
        entry.DiagnosticState.Split('\n').ShouldContain("StatusCode=" + (int)status);
        entry.DiagnosticState.Split('\n').ShouldContain("ResponseKind=" + kind);
        entry.DiagnosticState.Split('\n').ShouldContain("ResponseCode=" + code);
        entry.ExceptionText.ShouldBeNull();
        logger.Entries.ShouldNotContain(entry => entry.EventId.Id == 2002 || entry.EventId.Id == 2004);
        AssertSupportSafeDiagnostics(logger, "protected-response-marker", body);
        AssertSupportSafeDiagnostics(logger, protectedValues);
        return entry;
    }

    [Fact]
    public async Task StartAsync_when_cancelled_before_application_start_does_not_send_command()
    {
        // Arrange — handler honours cancellation
        bool httpCalled = false;
        var handler = new TestHttpMessageHandler((_, ct) =>
        {
            httpCalled = true;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });

        IServiceScopeFactory scopeFactory = CreateScopeFactory(handler);
        var lifetime = new TestHostApplicationLifetime();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions
        {
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
    public async Task Development_symmetric_credential_is_a_short_lived_delegated_token_for_the_configured_administrator()
    {
        string? authorization = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
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
        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
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
    public async Task Without_a_delegated_credential_the_command_is_not_sent(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        bool commandSent = false;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            commandSent = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        Dictionary<string, string?> settings = scenario switch
        {
            "no-credential" => [],
            "production-signing-key" => DevelopmentContract(),
            _ => new()
            {
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
    public async Task InvalidAuthorityConfigurationSendsNoHttpRequests(string environmentName, string? authority)
    {
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
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
        if (!string.IsNullOrWhiteSpace(authority))
        {
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
    public async Task ValidAuthorityPreservesRealmPathAndRequiresAdministratorSubject(string environmentName, string authority, bool subjectMatches)
    {
        ArgumentNullException.ThrowIfNull(authority);
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        string delegatedToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = subjectMatches ? administrator : "diagnostic-other-subject",
            },
        });
        int requestCount = 0;
        Uri? capturedUri = null;
        string? capturedForm = null;
        using var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestCount++;
            request.Method.ShouldBe(HttpMethod.Post);
            capturedUri = request.RequestUri;
            capturedForm = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { access_token = delegatedToken })),
            };
        });
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var provider = new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
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
        if (!subjectMatches)
        {
            logger.Messages.ShouldContain(static message => message.Contains("authority-subject-mismatch", StringComparison.Ordinal));
        }

        AssertSupportSafeDiagnostics(logger, administrator, username, password, delegatedToken, authority, "diagnostic-other-subject");
    }

    [Theory]
    [InlineData("Production", "")]
    [InlineData("Production", "   ")]
    [InlineData("Production", "connect/token")]
    [InlineData("Production", "https:///connect/token")]
    [InlineData("Production", "https://identity.example.test:invalid/token")]
    [InlineData("Production", "https://identity.example.test:65536/token")]
    [InlineData("Production", "https://identity.example.test/invalid token")]
    [InlineData("Production", "https://identity.example.test/%invalid")]
    [InlineData("Production", "https://identity.example.test/invalid\\token")]
    [InlineData("Production", "ftp://identity.example.test/token")]
    [InlineData("Production", "https://diagnostic-username:diagnostic-password@identity.example.test/token")]
    [InlineData("Production", "https://identity.example.test/token?secret=diagnostic-token")]
    [InlineData("Production", "https://identity.example.test/token#diagnostic-token")]
    [InlineData("Production", "http://localhost:18080/token")]
    [InlineData("Staging", "http://localhost:18080/token")]
    [InlineData("ReleaseCandidate", "http://localhost:18080/token")]
    [InlineData("Development", "")]
    [InlineData("Development", "connect/token")]
    [InlineData("Development", "http://diagnostic-username:diagnostic-password@localhost/token")]
    [InlineData("Development", "http://localhost/token?secret=diagnostic-token")]
    public async Task InvalidTokenEndpointSendsNoRequestsOrFallback(string environmentName, string endpoint)
    {
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref requestCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        Dictionary<string, string?> settings = AuthoritySettings();
        settings["EventStore:Authentication:TokenEndpoint"] = endpoint;
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime,
            logger,
            settings,
            environmentName);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2006));

        Volatile.Read(ref requestCount).ShouldBe(0);
        logger.Messages.ShouldContain(static message => message.Contains("token-endpoint-", StringComparison.Ordinal));
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, "diagnostic-token", AuthorityUrl);
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            AssertSupportSafeDiagnostics(logger, endpoint);
        }
    }

    [Theory]
    [InlineData("Production", null)]
    [InlineData("Production", "not-an-authority")]
    [InlineData("Production", "http://identity.example.test/realm")]
    [InlineData("Development", null)]
    [InlineData("Development", "")]
    [InlineData("Development", "   ")]
    public async Task ValidEndpointCannotBypassAuthorityContract(string environmentName, string? authority)
    {
        int requests = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var client = new HttpClient(handler);
        Dictionary<string, string?> settings = DevelopmentContract();
        settings["EventStore:Authentication:Authority"] = authority;
        settings["EventStore:Authentication:TokenEndpoint"] = "https://tokens.example.test/connect/token";
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        var provider = new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), environment, TimeProvider.System, logger);

        (await provider.AcquireAsync(AuthorityAdministrator, client, CancellationToken.None)).ShouldBeNull();

        requests.ShouldBe(0);
        logger.Entries.ShouldContain(static entry => entry.EventId.Id == 2006);
    }

    [Theory]
    [InlineData("Production", "https://tokens.example.test/oauth2/token", true)]
    [InlineData("Staging", "https://tokens.example.test/oauth2/token", true)]
    [InlineData("Development", "http://localhost:18080/custom/token", true)]
    [InlineData("Production", "https://tokens.example.test/oauth2/token", false)]
    public async Task ConfiguredTokenEndpointPreservesDelegatedSubjectAndCommandTarget(string environmentName, string endpoint, bool subjectMatches)
    {
        string token = CreateAuthorityToken(subjectMatches);
        int requests = 0;
        using var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
        {
            if (Interlocked.Increment(ref requests) == 1)
            {
                request.RequestUri.ShouldBe(new Uri(endpoint));
                request.Method.ShouldBe(HttpMethod.Post);
                string form = await request.Content!.ReadAsStringAsync(cancellationToken);
                form.ShouldContain("grant_type=password");
                form.ShouldContain($"username={AuthorityUsername}");
                form.ShouldContain($"password={AuthorityPassword}");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { access_token = token })),
                };
            }

            request.Headers.Authorization!.Parameter.ShouldBe(token);
            using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            body.RootElement.GetProperty("payload").GetProperty("UserId").GetString().ShouldBe(AuthorityAdministrator);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        Dictionary<string, string?> settings = AuthoritySettings();
        settings["EventStore:Authentication:TokenEndpoint"] = endpoint;
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime, logger, settings, environmentName);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id is 2001 or 2006 or 2002));

        Volatile.Read(ref requests).ShouldBe(subjectMatches ? 2 : 1);
        logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2002);
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, token, endpoint);
    }

    [Theory]
    [InlineData("credential-failure", 1, 2005)]
    [InlineData("command-failure", 2, 2003)]
    [InlineData("ambiguous-delivery", 2, 2002)]
    public async Task BootstrapMakesOneAttemptAndRequiresExplicitOperatorRecovery(string scenario, int expectedRequests, int eventId)
    {
        int requests = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            int requestNumber = Interlocked.Increment(ref requests);
            if (requestNumber == 1)
            {
                return Task.FromResult(new HttpResponseMessage(scenario == "credential-failure" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { access_token = CreateAuthorityToken() })),
                });
            }

            if (scenario == "ambiguous-delivery")
            {
                throw new HttpRequestException("diagnostic-password");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        await using AsyncServiceScope realScope = CreateScopeFactory(handler).CreateAsyncScope();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IServiceScope observedScope = Substitute.For<IServiceScope, IAsyncDisposable>();
        observedScope.ServiceProvider.Returns(realScope.ServiceProvider);
        ((IAsyncDisposable)observedScope).DisposeAsync().Returns(_ =>
        {
            completed.TrySetResult();
            return ValueTask.CompletedTask;
        });
        IServiceScopeFactory factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(observedScope);
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(factory,
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime, logger, AuthoritySettings(), Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        // Disposal precedes the outer catch diagnostic on ambiguous delivery.
        await WaitUntilAsync(() => logger.Entries.Any(entry => entry.EventId.Id == eventId));
        lifetime.StartApplication();
        await service.StopAsync(CancellationToken.None);

        Volatile.Read(ref requests).ShouldBe(expectedRequests);
        logger.Entries.ShouldContain(entry => entry.EventId.Id == eventId);
        logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2001);
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.OK)]
    public async Task AuthorityResponseDiagnosticsExcludeCredentialsAndTokens(HttpStatusCode statusCode)
    {
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        const string malformedToken = "diagnostic-token";
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
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
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
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
    public async Task BootstrapFailureDiagnosticsExcludeExceptionCredentialsAndTokens(string scenario, string phase, string exceptionType, int expectedRequests)
    {
        const string administrator = "diagnostic-administrator";
        const string username = "diagnostic-username";
        const string password = "diagnostic-password";
        const string authority = "https://identity.example.test/realms/hexalith";
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = administrator },
        });
        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) =>
        {
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
            new Dictionary<string, string?>
            {
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
    public async Task Authority_mode_sends_only_the_configured_administrators_own_token(bool subjectMatches)
    {
        string adminToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://identity.example.test/realms/hexalith",
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = subjectMatches ? "admin-user-1" : "someone-else" },
        });
        string? tokenRequest = null;
        string? commandAuthorization = null;
        var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                tokenRequest = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
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
            new Dictionary<string, string?>
            {
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
        if (subjectMatches)
        {
            commandAuthorization.ShouldBe("Bearer " + adminToken);
        }
        else
        {
            commandAuthorization.ShouldBeNull();
            logger.Messages.ShouldContain(static message => message.Contains("authority-subject-mismatch", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(65535, false)]
    [InlineData(65536, false)]
    [InlineData(65537, false)]
    [InlineData(65535, true)]
    [InlineData(65536, true)]
    [InlineData(65537, true)]
    public async Task AuthorityTokenResponseEnforcesByteLimit(int responseBytes, bool declaresLength)
    {
        string delegatedToken = CreateAuthorityToken();
        using var stream = new BootstrapTokenResponseStream(CreateTokenResponseBody(delegatedToken, responseBytes));
        using var content = new StreamContent(stream);
        if (declaresLength)
        {
            content.Headers.ContentLength = responseBytes;
        }

        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        }));
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(logger);

        if (responseBytes <= TokenResponseLimit)
        {
            string? acquiredToken = await provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None);
            acquiredToken.ShouldBe(delegatedToken);
            stream.BytesRead.ShouldBe(responseBytes);
        }
        else
        {
            await Should.ThrowAsync<HttpRequestException>(() => provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None));
            if (declaresLength)
            {
                stream.BytesRead.ShouldBe(0);
            }
        }

        stream.IsDisposed.ShouldBeTrue();
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, delegatedToken, AuthorityUrl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4096)]
    [InlineData(81920)]
    [InlineData(262144)]
    public async Task UnknownLengthAuthorityTokenResponseStopsBeforeDrainingOversizedBody(int readChunkSize)
    {
        string delegatedToken = CreateAuthorityToken();
        const int responseBytes = 1024 * 1024;
        using var stream = new BootstrapTokenResponseStream(CreateTokenResponseBody(delegatedToken, responseBytes), readChunkSize: readChunkSize);
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream, readChunkSize),
        }));
        using var httpClient = new HttpClient(handler);
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(new TestLogger<TenantBootstrapHostedService>());

        await Should.ThrowAsync<HttpRequestException>(() => provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None));

        stream.BytesRead.ShouldBeInRange(TokenResponseLimit, TokenResponseLimit + readChunkSize);
        stream.BytesRead.ShouldBeLessThan(responseBytes);
        stream.IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(65537)]
    [InlineData(1048576)]
    public async Task AuthorityTokenResponseEnforcesLimitDespiteUnderstatedContentLength(int responseBytes)
    {
        using var stream = new BootstrapTokenResponseStream(CreateTokenResponseBody(CreateAuthorityToken(), responseBytes));
        using var content = new StreamContent(stream);
        content.Headers.ContentLength = TokenResponseLimit - 1;
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        }));
        using var httpClient = new HttpClient(handler);
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(new TestLogger<TenantBootstrapHostedService>());

        await Should.ThrowAsync<HttpRequestException>(() => provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None));

        stream.BytesRead.ShouldBeInRange(TokenResponseLimit, TokenResponseLimit + 4096);
        stream.IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FailedAuthorityTokenResponseIsDisposedWithoutReadingBody(HttpStatusCode statusCode)
    {
        using var stream = new BootstrapTokenResponseStream(new byte[TokenResponseLimit * 4]);
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StreamContent(stream),
        }));
        using var httpClient = new HttpClient(handler);
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(logger);

        string? token = await provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None);

        token.ShouldBeNull();
        stream.BytesRead.ShouldBe(0);
        stream.IsDisposed.ShouldBeTrue();
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 2005 && entry.Message.Contains(((int)statusCode).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, AuthorityUrl);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorityTokenResponseReadHonorsCallerCancellation(bool infiniteClientTimeout)
    {
        using var stream = new BootstrapTokenResponseStream([], blockReads: true);
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream),
        }));
        using var httpClient = new HttpClient(handler);
        if (infiniteClientTimeout)
        {
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        using var cancellation = new CancellationTokenSource();
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(new TestLogger<TenantBootstrapHostedService>());
        Task<string?> acquisition = provider.AcquireAsync(AuthorityAdministrator, httpClient, cancellation.Token);
        await stream.ReadStarted.WaitAsync(TimeSpan.FromSeconds(10));
        acquisition.IsCompleted.ShouldBeFalse();

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => acquisition.WaitAsync(TimeSpan.FromSeconds(10)));

        stream.BytesRead.ShouldBe(0);
        stream.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorityTokenResponseReadSharesClientTimeoutWithHeaders()
    {
        using var stream = new BootstrapTokenResponseStream([], blockReads: true);
        using var handler = new TestHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(2),
        };
        TenantBootstrapCredentialProvider provider = CreateAuthorityCredentialProvider(new TestLogger<TenantBootstrapHostedService>());
        Task<string?> acquisition = provider.AcquireAsync(AuthorityAdministrator, httpClient, CancellationToken.None);
        await stream.ReadStarted.WaitAsync(TimeSpan.FromSeconds(10));

        // The headers used half the two-second budget. A new two-second body deadline would
        // exceed this watchdog and throw TimeoutException instead of operation cancellation.
        await Should.ThrowAsync<OperationCanceledException>(() => acquisition.WaitAsync(TimeSpan.FromMilliseconds(1500)));

        stream.IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task StreamedAuthorityTokenAtLimitDispatchesOnlyForConfiguredSubject(bool subjectMatches, bool declaresLength)
    {
        string delegatedToken = CreateAuthorityToken(subjectMatches);
        using var stream = new BootstrapTokenResponseStream(CreateTokenResponseBody(delegatedToken, TokenResponseLimit));
        using var content = new StreamContent(stream);
        if (declaresLength)
        {
            content.Headers.ContentLength = TokenResponseLimit;
        }

        int requestCount = 0;
        string? authorization = null;
        using var handler = new TestHttpMessageHandler((request, _) =>
        {
            if (++requestCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            }

            authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime,
            logger,
            AuthoritySettings(),
            Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id is 2001 or 2006));

        requestCount.ShouldBe(subjectMatches ? 2 : 1);
        authorization.ShouldBe(subjectMatches ? "Bearer " + delegatedToken : null);
        if (!subjectMatches)
        {
            logger.Messages.ShouldContain(static message => message.Contains("authority-subject-mismatch", StringComparison.Ordinal));
        }

        logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2002);
        stream.BytesRead.ShouldBe(TokenResponseLimit);
        stream.IsDisposed.ShouldBeTrue();
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, delegatedToken, AuthorityUrl, "diagnostic-other-subject", "diagnostic-body-secret");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedAuthorityTokenResponsePreventsBootstrapDispatchWithSafeDiagnostics(bool declaresLength)
    {
        string delegatedToken = CreateAuthorityToken();
        const int responseBytes = TokenResponseLimit * 4;
        using var stream = new BootstrapTokenResponseStream(CreateTokenResponseBody(delegatedToken, responseBytes));
        using var content = new StreamContent(stream);
        if (declaresLength)
        {
            content.Headers.ContentLength = responseBytes;
        }

        int requestCount = 0;
        using var handler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(++requestCount == 1 ? HttpStatusCode.OK : HttpStatusCode.Accepted)
        {
            Content = requestCount == 1 ? content : null,
        }));
        using var lifetime = new TestHostApplicationLifetime();
        var logger = new TestLogger<TenantBootstrapHostedService>();
        TenantBootstrapHostedService service = CreateService(
            CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime,
            logger,
            AuthoritySettings(),
            Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id is 2001 or 2002));

        requestCount.ShouldBe(1);
        TestLogEntry failure = logger.Entries.Single(static entry => entry.EventId.Id == 2002);
        failure.Message.ShouldContain("credential-acquisition");
        failure.Message.ShouldContain(nameof(HttpRequestException));
        failure.ExceptionText.ShouldBeNull();
        logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2001);
        stream.IsDisposed.ShouldBeTrue();
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, delegatedToken, AuthorityUrl, "diagnostic-body-secret");
    }

    [Fact]
    public async Task AmbiguousBootstrapDeliveryLogsExactAttemptIdentifiersBeforeDispatch()
    {
        string? messageId = null;
        string? correlationId = null;
        string delegatedToken = CreateAuthorityToken();
        int requests = 0;
        var logger = new TestLogger<TenantBootstrapHostedService>();
        using var handler = new TestHttpMessageHandler(async (request, cancellationToken) =>
        {
            if (Interlocked.Increment(ref requests) == 1)
            {
                logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2008);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { access_token = delegatedToken })),
                };
            }

            using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            messageId = body.RootElement.GetProperty("messageId").GetString();
            correlationId = body.RootElement.GetProperty("correlationId").GetString();
            TestLogEntry attempt = logger.Entries.Single(static entry => entry.EventId.Id == 2008);
            attempt.LogLevel.ShouldBe(LogLevel.Information);
            attempt.DiagnosticState.ShouldContain($"MessageId={messageId}");
            attempt.DiagnosticState.ShouldContain($"CorrelationId={correlationId}");
            throw new HttpRequestException($"Ambiguous delivery {AuthorityAdministrator} {AuthorityPassword} {delegatedToken}");
        });
        using var lifetime = new TestHostApplicationLifetime();
        TenantBootstrapHostedService service = CreateService(CreateScopeFactory(handler),
            Options.Create(new TenantBootstrapOptions { BootstrapGlobalAdminUserId = AuthorityAdministrator }),
            lifetime, logger, AuthoritySettings(), Environments.Production);

        await service.StartAsync(CancellationToken.None);
        lifetime.StartApplication();
        await WaitUntilAsync(() => logger.Entries.Any(static entry => entry.EventId.Id == 2002));

        Volatile.Read(ref requests).ShouldBe(2);
        messageId.ShouldNotBeNull().Length.ShouldBe(26);
        correlationId.ShouldNotBeNull().Length.ShouldBe(26);
        Should.NotThrow(() => UniqueIdHelper.ExtractTimestamp(messageId));
        Should.NotThrow(() => UniqueIdHelper.ExtractTimestamp(correlationId));
        TestLogEntry logged = logger.Entries.Single(static entry => entry.EventId.Id == 2008);
        logged.Message.ShouldContain(messageId);
        logged.Message.ShouldContain(correlationId);
        logger.Entries.ShouldNotContain(static entry => entry.EventId.Id == 2001);
        AssertSupportSafeDiagnostics(logger, AuthorityAdministrator, AuthorityUsername, AuthorityPassword, delegatedToken, AuthorityUrl);
    }

    private const int TokenResponseLimit = 64 * 1024;
    private const string AuthorityAdministrator = "diagnostic-administrator";
    private const string AuthorityUsername = "diagnostic-username";
    private const string AuthorityPassword = "diagnostic-password";
    private const string AuthorityUrl = "https://identity.example.test/realms/hexalith";

    private static Dictionary<string, string?> AuthoritySettings() => new(StringComparer.Ordinal)
    {
        ["EventStore:Authentication:Authority"] = AuthorityUrl,
        ["EventStore:Authentication:Username"] = AuthorityUsername,
        ["EventStore:Authentication:Password"] = AuthorityPassword,
    };

    private static TenantBootstrapCredentialProvider CreateAuthorityCredentialProvider(ILogger logger)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        return new TenantBootstrapCredentialProvider(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(AuthoritySettings(), StringComparer.Ordinal)
            {
                ["EventStore:Authentication:TokenEndpoint"] = "https://tokens.example.test/oauth2/token",
            }).Build(),
            environment,
            TimeProvider.System,
            logger);
    }

    private static string CreateAuthorityToken(bool subjectMatches = true) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = subjectMatches ? AuthorityAdministrator : "diagnostic-other-subject" },
    });

    private static byte[] CreateTokenResponseBody(string delegatedToken, int responseBytes)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(new { access_token = delegatedToken, secret = "diagnostic-body-secret" });
        var body = new byte[responseBytes];
        body.AsSpan().Fill((byte)' ');
        json.CopyTo(body, 0);
        return body;
    }

    private static void AssertSupportSafeDiagnostics(TestLogger<TenantBootstrapHostedService> logger, params string[] sensitiveValues)
    {
        foreach (TestLogEntry entry in logger.Entries)
        {
            string diagnostics = $"{entry.Message}\n{entry.DiagnosticState}\n{entry.ExceptionText}";
            foreach (string sensitiveValue in sensitiveValues)
            {
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
        => new(StringComparer.Ordinal)
        {
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
        string environmentName = "Development")
    {
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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, cts.Token);
        }
    }

    private static IServiceScopeFactory CreateScopeFactory(TestHttpMessageHandler handler)
    {
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
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public CancellationToken ApplicationStarted => _started.Token;

        public CancellationToken ApplicationStopping => _stopping.Token;

        public CancellationToken ApplicationStopped => _stopped.Token;

        public void StopApplication() => _stopping.Cancel();

        public void StartApplication() => _started.Cancel();

        public void Dispose()
        {
            _started.Dispose();
            _stopping.Dispose();
            _stopped.Dispose();
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<string> _messages = new();
        private readonly ConcurrentQueue<TestLogEntry> _entries = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();
        public IReadOnlyCollection<TestLogEntry> Entries => _entries.ToArray();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            _messages.Enqueue(message);
            string diagnosticState = state is IEnumerable<KeyValuePair<string, object?>> fields
                ? string.Join("\n", fields.Select(static field => $"{field.Key}={field.Value}"))
                : state?.ToString() ?? string.Empty;
            _entries.Enqueue(new TestLogEntry(logLevel, eventId, message, diagnosticState, exception?.ToString()));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, EventId EventId, string Message, string DiagnosticState, string? ExceptionText);
}
