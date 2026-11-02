using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Security;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Aggregates;

using SubmitPipelineCommand = Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public partial class CommandApiRuntimeIntegrationTests
{
    /// <summary>Refuses forged internal authority before protected routing or persistence can run.</summary>
    [Theory]
    [InlineData("proof", HttpStatusCode.Forbidden)]
    [InlineData("tampered-principal", HttpStatusCode.Forbidden)]
    [InlineData("missing-proof", HttpStatusCode.Forbidden)]
    [InlineData("caller", HttpStatusCode.Unauthorized)]
    [InlineData("untrusted-caller", HttpStatusCode.Unauthorized)]
    [InlineData("domain", HttpStatusCode.Forbidden)]
    [InlineData("missing-channel", HttpStatusCode.Unauthorized)]
    [InlineData("wrong-channel", HttpStatusCode.Unauthorized)]
    [InlineData("missing-assertion", HttpStatusCode.Unauthorized)]
    public async Task BoundBootstrapRefusesForgedAuthorityBeforeEffects(string scenario, HttpStatusCode expectedStatus)
    {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        ICommandStatusStore status = Substitute.For<ICommandStatusStore>();
        ICommandArchiveStore archive = Substitute.For<ICommandArchiveStore>();
        using var authority = new TenantGatewayBoundWorkloadAuthority(scenario == "untrusted-caller" ? "forged" : "tenants");
        await using var host = new TenantGatewayTestHost(router, status, archive, boundAuthority: authority);
        using HttpClient client = host.CreateClient();
        SubmitCommandRequest command = CreateBootstrapRequest() with
        {
            Domain = scenario == "domain" ? "tenants" : "global-administrators",
        };
        using HttpRequestMessage request = await authority.CreateRequestAsync(host.Services, CreateJwt("admin-1"), command);
        string? header = scenario switch
        {
            "proof" or "missing-proof" => BoundWorkloadCommandProof.HeaderName,
            "caller" => EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName,
            "missing-channel" or "wrong-channel" => DaprAppChannelToken.HeaderName,
            "missing-assertion" => EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName,
            _ => null,
        };
        if (header is not null)
        {
            request.Headers.Remove(header);
            if (scenario is "proof" or "caller" or "wrong-channel") request.Headers.Add(header, "forged");
        }

        if (scenario == "tampered-principal")
        {
            request.Content?.Dispose();
            request.Content = JsonContent.Create(new BoundWorkloadCommandRequest(command, "forged-admin"));
        }

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(expectedStatus);
        router.ReceivedCalls().ShouldBeEmpty();
        status.ReceivedCalls().ShouldBeEmpty();
        archive.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Invalid human signatures and name-only identities cannot produce represented-subject proofs.</summary>
    [Theory]
    [InlineData("signature")]
    [InlineData("name-only")]
    public async Task BoundBootstrapRequiresCryptographicallyValidatedHumanSubject(string scenario)
    {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        using var authority = new TenantGatewayBoundWorkloadAuthority();
        await using var host = new TenantGatewayTestHost(router, boundAuthority: authority);
        using HttpClient client = host.CreateClient();
        string token = scenario == "signature"
            ? CreateJwt("admin-1", signingKey: "a-different-signing-key-minimum-32-characters")
            : CreateJwtWithoutSub([new Claim("name", "admin-1")]);

        await Should.ThrowAsync<InvalidOperationException>(() => authority.CreateRequestAsync(host.Services, token,
            CreateBootstrapRequest() with { Domain = "global-administrators" }));

        router.ReceivedCalls().ShouldBeEmpty();
    }
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    /// <summary>Refuses internal bootstrap through the public route even for an otherwise authorized human administrator.</summary>
    [Fact]
    public async Task BoundBootstrapPublicHumanCannotInvokeInternalOperation()
    {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        ICommandStatusStore status = Substitute.For<ICommandStatusStore>();
        ICommandArchiveStore archive = Substitute.For<ICommandArchiveStore>();
        await using var host = new TenantGatewayTestHost(router, status, archive);
        string token = CreateJwt("admin-1", claims:
        [
            new Claim("eventstore:tenant", "system"),
            new Claim("eventstore:domain", "global-administrators"),
            new Claim("eventstore:permission", nameof(BootstrapGlobalAdmin)),
            new Claim("global_admin", "true"),
        ]);
        using HttpClient client = CreateClientWithBearer(host, token);
        SubmitCommandRequest command = CreateBootstrapRequest() with { Domain = "global-administrators" };

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", command);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        router.ReceivedCalls().ShouldBeEmpty();
        status.ReceivedCalls().ShouldBeEmpty();
        archive.ReceivedCalls().ShouldBeEmpty();
    }
#endif

    /// <summary>Composes the real creator and receiving gateway with the real empty-state aggregate.</summary>
    [Fact]
    public async Task BoundBootstrapCreatorAndGatewayAcceptMatchingValidatedAdministrator()
    {
        const string actorUserId = "admin-1";
        var aggregate = new GlobalAdministratorsAggregate();
        SubmitPipelineCommand? dispatchedCommand = null;
        DomainResult? domainResult = null;
        ICommandRouter router = Substitute.For<ICommandRouter>();
        router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                SubmitPipelineCommand submitted = call.ArgAt<SubmitPipelineCommand>(0);
                dispatchedCommand = submitted;
                var envelope = new CommandEnvelope(submitted.MessageId, submitted.Tenant, submitted.Domain,
                    submitted.AggregateId, submitted.CommandType, submitted.Payload, submitted.CorrelationId,
                    null, submitted.UserId, submitted.Extensions);
                domainResult = await aggregate.ProcessAsync(envelope, currentState: null);
                return new CommandProcessingResult(domainResult.IsSuccess, null, submitted.CorrelationId);
            });
        ICommandStatusStore status = Substitute.For<ICommandStatusStore>();
        status.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow,
                "global-administrators", 1, null, null, null));
        using var authority = new TenantGatewayBoundWorkloadAuthority();
        await using var host = new TenantGatewayTestHost(router, status,
            Substitute.For<ICommandArchiveStore>(), boundAuthority: authority);
        using HttpClient client = host.CreateClient();
        string token = CreateJwt(actorUserId, claims:
        [
            new Claim("name", "untrusted-display-name"),
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "true"),
        ]);
        SubmitCommandRequest command = CreateBootstrapRequest() with { Domain = "global-administrators" };
        using HttpRequestMessage request = (await authority.CreateProductionBootstrapRequestAsync(host.Services,
            token, command, actorUserId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        dispatchedCommand.ShouldNotBeNull().UserId.ShouldBe(actorUserId);
        dispatchedCommand.AuthenticatedSubject.ShouldBe(actorUserId);
        dispatchedCommand.IsGlobalAdmin.ShouldBeFalse();
        dispatchedCommand.Extensions.ShouldNotBeNull()[EventStoreGatewayVerifiedOrigin.ExtensionKey].ShouldBe("tenants");
        dispatchedCommand.Extensions[EventStoreGatewayVerifiedOrigin.ActorExtensionKey].ShouldBe(actorUserId);
        domainResult.ShouldNotBeNull().IsSuccess.ShouldBeTrue();
        domainResult.IsRejection.ShouldBeFalse();
        GlobalAdministratorSet created = domainResult.Events.ShouldHaveSingleItem().ShouldBeOfType<GlobalAdministratorSet>();
        created.TenantId.ShouldBe("system");
        created.UserId.ShouldBe(actorUserId);
        created.ActorUserId.ShouldBe(actorUserId);
    }

}
