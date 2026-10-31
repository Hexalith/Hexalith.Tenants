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
        using var request = await TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
            "bootstrap-administrator", bearer, TimeProvider.System, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        if (scenario is not ("valid" or "web-payload")) { request.ShouldBeNull(); return; }
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
            ValidateIssuer = true, ValidIssuer = "bootstrap-origin", ValidateAudience = true, ValidAudience = "eventstore",
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ValidateLifetime = true,
        });
        validated.IsValid.ShouldBeTrue();
        validated.ClaimsIdentity.FindFirst("azp")!.Value.ShouldBe("tenants");
        validated.ClaimsIdentity.FindFirst(EventStoreWorkloadAuthenticationDefaults.OperationClaimType)!.Value.ShouldBe(EventStoreWorkloadOperations.GatewayCommandSubmit);
    }
}
