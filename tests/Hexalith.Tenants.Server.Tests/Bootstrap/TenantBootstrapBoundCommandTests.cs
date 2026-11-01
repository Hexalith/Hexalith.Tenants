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
    [InlineData("selected-target")]
    [InlineData("selected-caller")]
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
    [InlineData("buffer-limit")]
    [InlineData("buffer-cancellation")]
    public async Task OnlyTheValidatedAdministratorCanBindTheExactInternalBootstrap(string scenario)
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
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
        if (scenario == "buffer-cancellation")
        {
            var issuer = Substitute.For<IWorkloadAssertionIssuer>();
            issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                caller.Cancel();
                return ValueTask.FromResult<string?>("test-assertion-never-dispatched");
            });
            services.AddSingleton(issuer);
        }
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
        if (scenario == "buffer-limit")
        {
            command = command with { Extensions = new Dictionary<string, string> { ["oversized"] = new string('x', 1_048_576) } };
            await Should.ThrowAsync<HttpRequestException>(() => TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
                "bootstrap-administrator", bearer, TimeProvider.System, TimeSpan.FromSeconds(1), caller.Token));
            return;
        }
        if (scenario == "buffer-cancellation")
        {
            await Should.ThrowAsync<OperationCanceledException>(() => TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
                "bootstrap-administrator", bearer, TimeProvider.System, TimeSpan.FromSeconds(1), caller.Token));
            caller.IsCancellationRequested.ShouldBeTrue();
            return;
        }
        using var request = await TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration, dapr, command,
            "bootstrap-administrator", bearer, TimeProvider.System, TimeSpan.FromSeconds(1), caller.Token);
        if (scenario is not ("valid" or "web-payload" or "selected-target" or "selected-caller")) { request.ShouldBeNull(); return; }
        request.ShouldNotBeNull(); request.RequestUri!.AbsolutePath.ShouldEndWith("/api/v1/commands/bound/workload");
        request.RequestUri.AbsolutePath.ShouldContain("/invoke/" + (scenario == "selected-target" ? "eventstore-isolated" : "eventstore") + "/method/");
        request.Headers.Authorization.ShouldBeNull();
        byte[] expectedBytes = JsonSerializer.SerializeToUtf8Bytes(new BoundWorkloadCommandRequest(command, "bootstrap-administrator"), JsonSerializerOptions.Web);
        request.Content.ShouldNotBeNull();
        request.Content.Headers.ContentLength.ShouldBe((long)expectedBytes.Length);
        expectedBytes.Length.ShouldBeGreaterThan(0);
        request.Headers.TransferEncodingChunked.ShouldNotBe(true);
        using MemoryStream firstWrite = new();
        using MemoryStream repeatedWrite = new();
        await request.Content.CopyToAsync(firstWrite, caller.Token);
        await request.Content.CopyToAsync(repeatedWrite, caller.Token);
        firstWrite.ToArray().ShouldBe(expectedBytes);
        repeatedWrite.ToArray().ShouldBe(expectedBytes);
        var bound = (await request.Content!.ReadFromJsonAsync<BoundWorkloadCommandRequest>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
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
        if (scenario == "valid") await ObserveHttpFramingAsync(request, expectedBytes, caller.Token);
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
}
