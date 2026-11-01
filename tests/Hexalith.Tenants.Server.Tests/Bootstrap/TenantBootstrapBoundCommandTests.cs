using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    [InlineData("workload")]
    [InlineData("payload")]
    [InlineData("missing-key")]
    [InlineData("missing-caller")]
    public async Task OnlyTheValidatedAdministratorCanBindTheExactInternalBootstrap(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using RSA proofKey = RSA.Create(2048);
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "bootstrap-origin",
            ["Authentication:JwtBearer:Audience"] = "bootstrap-reader",
            ["Authentication:JwtBearer:SigningKey"] = key,
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["Authentication:WorkloadIssuer:Workload"] = scenario == "missing-caller" ? null : "tenants",
            ["Authentication:BoundStreamRead:PrivateKey"] = scenario == "missing-key" ? null : Convert.ToBase64String(proofKey.ExportPkcs8PrivateKey()),
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(configuration);
        IHostEnvironment environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName.Returns(Environments.Development);
        services.AddSingleton(environment);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            JwtBearerAuthenticationContract.Configure(options, configuration.GetSection("Authentication:JwtBearer").Get<JwtBearerAuthenticationOptions>()!));
        services.AddEventStoreWorkloadAssertionIssuer(configuration);
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
        if (scenario == "workload") claims.Add(new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "tenants"));
        var jwt = new JwtSecurityToken(scenario == "issuer" ? "foreign" : "bootstrap-origin",
            scenario == "audience" ? "foreign" : "bootstrap-reader", claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10), expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(2),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(scenario == "signature" ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) : key)), SecurityAlgorithms.HmacSha256));
        string bearer = new JwtSecurityTokenHandler().WriteToken(jwt);
        var command = new SubmitCommandRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", TenantIdentity.DefaultTenantId,
            TenantIdentity.GlobalAdministratorsDomain, TenantIdentity.GlobalAdministratorsAggregateId, nameof(BootstrapGlobalAdmin),
            JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin(scenario == "payload" ? "foreign" : "bootstrap-administrator"), scenario == "web-payload" ? JsonSerializerOptions.Web : JsonSerializerOptions.Default), "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        using var callerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        TimeSpan readinessTimeout = scenario.StartsWith("infinite", StringComparison.Ordinal)
            ? provider.GetRequiredService<IHttpClientFactory>().CreateClient().Timeout
            : TimeSpan.FromSeconds(1);
        if (scenario.StartsWith("infinite", StringComparison.Ordinal)) readinessTimeout.ShouldBe(Timeout.InfiniteTimeSpan);
        Task<HttpRequestMessage?> creation = TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
            "bootstrap-administrator", bearer, clock, readinessTimeout, callerCancellation.Token);
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
        if (scenario is not ("valid" or "web-payload" or "infinite-timeout" or "finite-catalog-ready" or "infinite-catalog-ready")) { request.ShouldBeNull(); return; }
        request.ShouldNotBeNull(); request.RequestUri!.AbsolutePath.ShouldEndWith("/api/v1/commands/bound/workload");
        request.Headers.Authorization.ShouldBeNull();
        var bound = (await request.Content!.ReadFromJsonAsync<BoundWorkloadCommandRequest>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        bound.PrincipalId.ShouldBe("bootstrap-administrator"); bound.Command.MessageId.ShouldBe(command.MessageId);
        string proof = request.Headers.GetValues(BoundWorkloadCommandProof.HeaderName).ShouldHaveSingleItem();
        string publicKey = Convert.ToBase64String(proofKey.ExportSubjectPublicKeyInfo());
        BoundWorkloadCommandProof.Verify(bound, proof, "tenants", publicKey, DateTimeOffset.UtcNow).ShouldBeTrue();
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
        validated.ClaimsIdentity.FindFirst("azp")!.Value.ShouldBe("tenants");
        validated.ClaimsIdentity.FindFirst(EventStoreWorkloadAuthenticationDefaults.OperationClaimType)!.Value.ShouldBe(EventStoreWorkloadOperations.GatewayCommandSubmit);
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
