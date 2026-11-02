using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Validates actual bearer signatures before the existing RSA requester proof and SDK workload assertion are issued; no sidecar is started.</summary>
public sealed class TenantBootstrapBoundCommandTests
{
    [Theory]
    [InlineData("valid")]
    [InlineData("web-payload")]
    [InlineData("infinite-timeout")]
    [InlineData("loopback")]
    [InlineData("loopback-web")]
    [InlineData("cancel-before-buffer")]
    [InlineData("selected-target")]
    [InlineData("selected-caller")]
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    [InlineData("finite-catalog-ready")]
    [InlineData("infinite-catalog-ready")]
    [InlineData("finite-catalog-timeout")]
    [InlineData("infinite-catalog-cancel")]
    [InlineData("infinite-catalog-deadline")]
#endif
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("subject")]
    [InlineData("not-admin")]
    [InlineData("missing-permission")]
    [InlineData("workload")]
    [InlineData("payload")]
    [InlineData("missing-key")]
    [InlineData("missing-caller")]
    [InlineData("buffer-limit")]
    [InlineData("buffer-cancellation")]
    public async Task OnlyTheValidatedAdministratorCanBindTheExactInternalBootstrap(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        using var callerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using RSA proofKey = RSA.Create(2048);
        var settings = new Dictionary<string, string?>
        {
            ["Tenants:EventStoreAppId"] = scenario == "selected-target" ? "eventstore-isolated" : null,
            ["Authentication:JwtBearer:Issuer"] = "bootstrap-origin",
            ["Authentication:JwtBearer:Audience"] = "bootstrap-reader",
            ["Authentication:JwtBearer:SigningKey"] = key,
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["Authentication:WorkloadIssuer:Workload"] = scenario == "missing-caller" ? null : scenario == "selected-caller" ? "tenants-isolated" : "tenants",
            ["Authentication:BoundStreamRead:PrivateKey"] = scenario == "missing-key" ? null : Convert.ToBase64String(proofKey.ExportPkcs8PrivateKey()),
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(configuration);
        IHostEnvironment environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns(Environments.Development);
        services.AddSingleton(environment);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            JwtBearerAuthenticationContract.Configure(options, configuration.GetSection("Authentication:JwtBearer").Get<JwtBearerAuthenticationOptions>()!));
        services.AddEventStoreWorkloadAssertionIssuer(configuration);
        if (scenario is "cancel-before-buffer" or "buffer-cancellation")
        {
            var issuer = Substitute.For<IWorkloadAssertionIssuer>();
            issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                callerCancellation.Cancel();
                return new ValueTask<string?>("cancelled-fixture-assertion");
            });
            services.AddSingleton(issuer);
        }
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        TimeProvider clock = TimeProvider.System;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Action? fireReadinessDeadline = null;
        TimeSpan? timerDueTime = null;
        TimeSpan? timerPeriod = null;
        ITimer? controlledTimer = null;
        if (scenario is "finite-catalog-timeout" or "infinite-catalog-deadline")
        {
            clock = Substitute.For<TimeProvider>();
            controlledTimer = Substitute.For<ITimer>();
            clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(call =>
                {
                    timerDueTime = call.ArgAt<TimeSpan>(2);
                    timerPeriod = call.ArgAt<TimeSpan>(3);
                    fireReadinessDeadline = () => call.ArgAt<TimerCallback>(0)(call.ArgAt<object?>(1));
                    return controlledTimer;
                });
        }
        if (scenario.Contains("catalog", StringComparison.Ordinal))
        {
            configuration["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] = "bootstrap-root";
            var authority = Substitute.For<IRouteCatalogAuthority>();
            authority.ReadbackAsync(Arg.Any<CancellationToken>()).Returns(async call =>
            {
                readStarted.TrySetResult();
                if (scenario is "finite-catalog-timeout" or "infinite-catalog-cancel" or "infinite-catalog-deadline")
                    await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return new RouteCatalogGenerationReadback(1, 1, "bootstrap-root", "routes", "admission", "development", [], true);
            });
            services.AddSingleton(authority);
        }
#endif
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint("http://127.0.0.1:3500").Build();
        string subject = scenario == "subject" ? "foreign-administrator" : "bootstrap-administrator";
        List<Claim> claims = [new("sub", subject)];
        if (scenario != "not-admin") claims.Add(new("global_admin", "true"));
        if (scenario != "missing-permission") claims.Add(new("eventstore:permission", "global-administrators:bootstrap"));
        if (scenario == "workload") claims.Add(new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "tenants"));
        var jwt = new JwtSecurityToken(scenario == "issuer" ? "foreign" : "bootstrap-origin",
            scenario == "audience" ? "foreign" : "bootstrap-reader", claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10), expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(2),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(scenario == "signature" ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) : key)), SecurityAlgorithms.HmacSha256));
        string bearer = new JwtSecurityTokenHandler().WriteToken(jwt);
        var command = new SubmitCommandRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", TenantPlatformScopes.GlobalAdministrators,
            TenantIdentity.GlobalAdministratorsDomain, TenantIdentity.GlobalAdministratorsAggregateId, nameof(BootstrapGlobalAdmin),
            JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin(scenario == "payload" ? "foreign" : "bootstrap-administrator"), scenario is "web-payload" or "loopback-web" ? JsonSerializerOptions.Web : JsonSerializerOptions.Default), "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        if (scenario == "buffer-limit")
        {
            command = command with { Extensions = new Dictionary<string, string> { ["oversized"] = new string('x', 1_048_576) } };
            await Should.ThrowAsync<HttpRequestException>(() => TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
                "bootstrap-administrator", bearer, clock, TimeSpan.FromSeconds(1), callerCancellation.Token));
            return;
        }
        TimeSpan readinessTimeout = scenario.StartsWith("infinite", StringComparison.Ordinal)
            ? provider.GetRequiredService<IHttpClientFactory>().CreateClient().Timeout
            : TimeSpan.FromSeconds(1);
        if (scenario.StartsWith("infinite", StringComparison.Ordinal)) readinessTimeout.ShouldBe(Timeout.InfiniteTimeSpan);
        Task<HttpRequestMessage?> creation = TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
            "bootstrap-administrator", bearer, clock, readinessTimeout, callerCancellation.Token);
        if (scenario is "cancel-before-buffer" or "buffer-cancellation")
        {
            await Should.ThrowAsync<OperationCanceledException>(() => creation);
            return;
        }
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        if (scenario is "finite-catalog-timeout" or "infinite-catalog-cancel" or "infinite-catalog-deadline")
        {
            await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (scenario == "infinite-catalog-cancel")
            {
                callerCancellation.Cancel();
            }
            else
            {
                // Assert the production timer's requested budget, then fire it with the
                // caller still active. A longer fallback or an unlinked timer must fail.
                timerDueTime.ShouldBe(scenario == "infinite-catalog-deadline" ? TimeSpan.FromSeconds(100) : TimeSpan.FromSeconds(1));
                timerPeriod.ShouldBe(Timeout.InfiniteTimeSpan);
                callerCancellation.IsCancellationRequested.ShouldBeFalse();
                creation.IsCompleted.ShouldBeFalse();
                fireReadinessDeadline.ShouldNotBeNull().Invoke();
            }
            await Should.ThrowAsync<OperationCanceledException>(() => creation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            if (scenario != "infinite-catalog-cancel")
            {
                callerCancellation.IsCancellationRequested.ShouldBeFalse();
                controlledTimer.ShouldNotBeNull().Received().Dispose();
            }
            return;
        }
#endif
        using var request = await creation;
        if (scenario is not ("valid" or "web-payload" or "infinite-timeout" or "finite-catalog-ready" or "infinite-catalog-ready" or "loopback" or "loopback-web" or "selected-target" or "selected-caller")) { request.ShouldBeNull(); return; }
        request.ShouldNotBeNull(); request.RequestUri!.AbsolutePath.ShouldEndWith("/api/v1/commands/bound/workload");
        request.RequestUri.AbsolutePath.ShouldContain("/invoke/" + (scenario == "selected-target" ? "eventstore-isolated" : "eventstore") + "/method/");
        request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization.Parameter.ShouldBe(bearer);
        request.Headers.GetValues("X-Hexalith-Scope").ShouldHaveSingleItem().ShouldBe(TenantPlatformScopes.GlobalAdministrators);
        HttpContent content = request.Content.ShouldNotBeNull();
        // Read length before ANY target-content read: a read can itself buffer JsonContent
        // and would hide the non-replayable request stream this regression guards.
        long declaredLength = content.Headers.ContentLength.ShouldNotBeNull();
        declaredLength.ShouldBeGreaterThan(0L);
        content.Headers.ContentType!.ToString().ShouldBe("application/json; charset=utf-8");
        using var expectedContent = JsonContent.Create(new BoundWorkloadCommandRequest(command, "bootstrap-administrator"));
        byte[] expectedBytes = await expectedContent.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        declaredLength.ShouldBe(expectedBytes.LongLength);
        if (scenario.StartsWith("loopback", StringComparison.Ordinal))
            await AssertKnownLengthReplayOverLoopbackAsync(request, expectedBytes, TestContext.Current.CancellationToken);
        byte[] actualBytes = await content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        actualBytes.ShouldBe(expectedBytes);
        (await content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBe(actualBytes);
        var bound = (await content.ReadFromJsonAsync<BoundWorkloadCommandRequest>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        bound.PrincipalId.ShouldBe("bootstrap-administrator"); bound.Command.MessageId.ShouldBe(command.MessageId);
        string proof = request.Headers.GetValues(BoundWorkloadCommandProof.HeaderName).ShouldHaveSingleItem();
        string publicKey = Convert.ToBase64String(proofKey.ExportSubjectPublicKeyInfo());
        BoundWorkloadCommandProof.Verify(bound, proof, scenario == "selected-caller" ? "tenants-isolated" : "tenants", publicKey, DateTimeOffset.UtcNow).ShouldBeTrue();
        BoundWorkloadCommandProof.Verify(bound with { PrincipalId = "foreign" }, proof, "tenants", publicKey, DateTimeOffset.UtcNow).ShouldBeFalse();
        string assertion = request.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldHaveSingleItem();
        var validated = await new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ValidateTokenAsync(assertion, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "bootstrap-origin",
            ValidateAudience = true,
            ValidAudience = "eventstore",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
        });
        validated.IsValid.ShouldBeTrue();
        validated.ClaimsIdentity.FindFirst("azp")!.Value.ShouldBe(scenario == "selected-caller" ? "tenants-isolated" : "tenants");
        validated.ClaimsIdentity.FindFirst(EventStoreWorkloadAuthenticationDefaults.OperationClaimType)!.Value.ShouldBe(EventStoreWorkloadOperations.GatewayCommandSubmit);
        if (scenario == "valid") await ObserveHttpFramingAsync(request, expectedBytes, callerCancellation.Token);
    }

    // A loopback HTTP receiver observes actual framing, without starting Dapr or invoking a command gateway.
    private static async Task ObserveHttpFramingAsync(HttpRequestMessage request, byte[] expectedBytes, CancellationToken token)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        request.RequestUri = new UriBuilder(request.RequestUri!)
        {
            Host = "127.0.0.1",
            Port = ((IPEndPoint)listener.LocalEndpoint).Port,
        }.Uri;

        async Task<(string Headers, byte[] Body)> ReceiveAsync()
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(deadline.Token);
            using NetworkStream stream = connection.GetStream();
            using MemoryStream headers = new();
            byte[] singleByte = new byte[1];
            while (headers.Length < 65_536)
            {
                await stream.ReadExactlyAsync(singleByte, deadline.Token);
                headers.WriteByte(singleByte[0]);
                if (headers.Length >= 4 && headers.GetBuffer().AsSpan((int)headers.Length - 4, 4).SequenceEqual("\r\n\r\n"u8)) break;
            }
            byte[] body = new byte[expectedBytes.Length];
            await stream.ReadExactlyAsync(body, deadline.Token);
            await stream.WriteAsync("HTTP/1.1 202 Accepted\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), deadline.Token);
            return (Encoding.ASCII.GetString(headers.ToArray()), body);
        }

        Task<(string Headers, byte[] Body)> received = ReceiveAsync();
        using HttpClient transport = new();
        using HttpResponseMessage response = await transport.SendAsync(request, deadline.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (string headers, byte[] body) = await received;
        headers.ShouldContain("Content-Length: " + expectedBytes.Length + "\r\n");
        headers.Contains("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        body.ShouldBe(expectedBytes);
    }

    private static async Task AssertKnownLengthReplayOverLoopbackAsync(HttpRequestMessage original, byte[] expectedBytes, CancellationToken token)
    {
        var observed = new ConcurrentQueue<(long? Length, string TransferEncoding, string? ContentType, string Protocol, string Proof, string Assertion, byte[] Body)>();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication receiver = builder.Build();
        receiver.Run(async context =>
        {
            long? length = context.Request.ContentLength;
            string transferEncoding = context.Request.Headers.TransferEncoding.ToString();
            using var body = new MemoryStream();
            await context.Request.Body.CopyToAsync(body, context.RequestAborted);
            observed.Enqueue((length, transferEncoding, context.Request.ContentType, context.Request.Protocol,
                context.Request.Headers[BoundWorkloadCommandProof.HeaderName].ToString(),
                context.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].ToString(), body.ToArray()));
            context.Response.StatusCode = StatusCodes.Status202Accepted;
        });
        await receiver.StartAsync(token);
        try
        {
            string address = receiver.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var endpoint = new Uri(new Uri(address), original.RequestUri!.PathAndQuery);
            using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false });
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using var outgoing = new HttpRequestMessage(original.Method, endpoint)
                {
                    Content = original.Content,
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };
                foreach (var header in original.Headers) outgoing.Headers.TryAddWithoutValidation(header.Key, header.Value).ShouldBeTrue();
                try
                {
                    using HttpResponseMessage response = await client.SendAsync(outgoing, token);
                    response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                }
                finally
                {
                    // Both sends use the same production content instance; its original
                    // request retains ownership after each temporary HTTP envelope.
                    outgoing.Content = null;
                }
            }
            observed.Count.ShouldBe(2);
            string proof = original.Headers.GetValues(BoundWorkloadCommandProof.HeaderName).ShouldHaveSingleItem();
            string assertion = original.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldHaveSingleItem();
            foreach (var request in observed)
            {
                request.Length.ShouldBe(expectedBytes.LongLength);
                request.TransferEncoding.ShouldBeEmpty();
                request.ContentType.ShouldBe("application/json; charset=utf-8");
                request.Protocol.ShouldBe("HTTP/1.1");
                request.Proof.ShouldBe(proof);
                request.Assertion.ShouldBe(assertion);
                request.Body.ShouldBe(expectedBytes);
            }
        }
        finally
        {
            await receiver.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>Rejects invalid readiness budgets before authentication or command construction.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task InvalidReadinessTimeoutsAreRefused(int milliseconds)
    {
        var error = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => TenantBootstrapBoundCommand.CreateAsync(
            null!, null!, null!, null!, "administrator", "credential", TimeProvider.System,
            TimeSpan.FromMilliseconds(milliseconds), TestContext.Current.CancellationToken));
        error.ParamName.ShouldBe("readinessTimeout");
    }

    /// <summary>Honors an already-cancelled caller before authentication or command construction.</summary>
    [Fact]
    public async Task CancelledBootstrapDoesNotBeginAuthentication()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => TenantBootstrapBoundCommand.CreateAsync(
            null!, null!, null!, null!, "administrator", "credential", TimeProvider.System,
            Timeout.InfiniteTimeSpan, cancellation.Token));
    }
}
