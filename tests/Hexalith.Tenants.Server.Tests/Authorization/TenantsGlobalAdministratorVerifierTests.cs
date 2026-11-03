using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Server.Tests.Support;
using NSubstitute;
using Shouldly;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Server.Tests.Authorization;

public sealed class TenantsGlobalAdministratorVerifierTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifierConsumesTheSharedSourceAuthority(bool allowed)
    {
        var authority = Substitute.For<ITenantsGlobalAdministratorAuthority>();
        authority.IsCurrentGlobalAdministratorAsync("admin", Arg.Any<CancellationToken>()).Returns(allowed);
        var claim = new DomainServiceAdministratorClaim("system", "tenants", "admin", "correlation");
        (await new TenantsGlobalAdministratorVerifier(authority).IsCurrentGlobalAdministratorAsync(claim,
            TestContext.Current.CancellationToken)).ShouldBe(allowed);
    }
    [Theory]
    [InlineData("admin", true)]
    [InlineData("other", false)]
    public async Task SdkRebuildConfirmsAdministratorOnlyFromNativeOwnerSource(string actor, bool allowed)
    {
        var source = GlobalAdministratorSourceFixture.Source();
        var authority = GlobalAdministratorSourceFixture.Authority(source, out var reader, out var metadata);
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<ITenantsGlobalAdministratorAuthority>(authority)
            .AddSingleton<IDomainServiceAdministratorVerifier, TenantsGlobalAdministratorVerifier>()
            .BuildServiceProvider();
        DomainServiceRequest request = CreateAssertedRequest(actor);
        byte[] expectedPayload = request.Command.Payload.ToArray();
        var expectedExtensions = new Dictionary<string, string>(request.Command.Extensions!);
        if (!allowed) expectedExtensions.Remove(DomainServiceAdministratorAssertions.GlobalAdminExtensionKey);

        DomainServiceRequest rebuilt = await DomainServiceAdministratorAssertions.RebuildAsync(services, request,
            TestContext.Current.CancellationToken);

        rebuilt.Command.Extensions.ShouldNotBeNull().OrderBy(item => item.Key, StringComparer.Ordinal)
            .ShouldBe(expectedExtensions.OrderBy(item => item.Key, StringComparer.Ordinal));
        rebuilt.Command.Payload.ShouldBe(expectedPayload);
        rebuilt.Command.ShouldBe(request.Command with { Extensions = rebuilt.Command.Extensions });
        rebuilt.ShouldBe(request with { Command = rebuilt.Command });
        request.Command.Extensions![DomainServiceAdministratorAssertions.GlobalAdminExtensionKey].ShouldBe("true");
        await reader.Received(1).ReadAsync(source.Identity, TestContext.Current.CancellationToken);
        await metadata.Received(1).ReadAsync(source.Identity, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SdkRebuildFailsClosedWhenNativeOwnerSourceIsUnavailable()
    {
        var source = GlobalAdministratorSourceFixture.Source();
        var authority = GlobalAdministratorSourceFixture.Authority(source, out var reader, out var metadata);
        reader.ReadAsync(source.Identity, Arg.Any<CancellationToken>())
            .Returns(new AuthoritativeStreamReadResult(null, "source-unavailable"));
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<ITenantsGlobalAdministratorAuthority>(authority)
            .AddSingleton<IDomainServiceAdministratorVerifier, TenantsGlobalAdministratorVerifier>()
            .BuildServiceProvider();
        DomainServiceRequest request = CreateAssertedRequest("admin");

        Exception failure = await Should.ThrowAsync<Exception>(() => DomainServiceAdministratorAssertions.RebuildAsync(
            services, request, TestContext.Current.CancellationToken));

        failure.GetType().ShouldBe(typeof(DomainServiceAdministratorAssertions).Assembly.GetType(
            "Hexalith.EventStore.DomainService.DomainServiceAdministratorVerificationException", throwOnError: true));
        failure.InnerException.ShouldBeOfType<TenantsGlobalAdministratorAuthorityUnavailableException>();
        request.Command.Extensions![DomainServiceAdministratorAssertions.GlobalAdminExtensionKey].ShouldBe("true");
        await reader.Received(1).ReadAsync(source.Identity, TestContext.Current.CancellationToken);
        await metadata.DidNotReceiveWithAnyArgs().ReadAsync(default!);
    }

    private static DomainServiceRequest CreateAssertedRequest(string actor)
        => new(new CommandEnvelope("01ARZ3NDEKTSV4RRFFQ69G5FAV", "system", "tenants", "acme", "UpdateTenant",
            """{"TenantId":"acme","Name":"Native assertion","Description":"Owner authority"}"""u8.ToArray(),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW", "01ARZ3NDEKTSV4RRFFQ69G5FAX", actor,
            new Dictionary<string, string>
            {
                [DomainServiceAdministratorAssertions.GlobalAdminExtensionKey] = "true",
                ["trace-id"] = "native-assertion-control",
                ["identity:surface"] = "agent",
            }), new object())
        {
            IncludePostCommandState = true,
            SnapshotCaptureProfileId = "assertion-control",
        };

    [Theory]
    [InlineData("canonical", true)]
    [InlineData("no-endpoint", false)]
    [InlineData("wrong-route", false)]
    [InlineData("wrong-method", false)]
    [InlineData("wrong-request-method", false)]
    [InlineData("missing-policy", false)]
    [InlineData("wrong-policy", false)]
    public async Task OnlyMatchedCanonicalQueryMetadataCanDiscardTheUnusedHint(string contextKind, bool stripped)
    {
        var authority = Substitute.For<ITenantsGlobalAdministratorAuthority>();
        authority.IsCurrentGlobalAdministratorAsync("admin", Arg.Any<CancellationToken>()).Returns(false);
        var context = new DefaultHttpContext();
        context.Request.Method = contextKind == "wrong-request-method" ? HttpMethods.Get : HttpMethods.Post;
        context.Request.Path = "/query";
        context.User = new(new System.Security.Claims.ClaimsIdentity([
            new("global_admin", "true"), new("operation", "domain-service:query")], "spoofed"));
        if (contextKind != "no-endpoint")
        {
            List<object> metadata = [new HttpMethodMetadata([contextKind == "wrong-method" ? HttpMethods.Get : HttpMethods.Post])];
            if (contextKind != "missing-policy") metadata.Add(new AuthorizeAttribute(contextKind == "wrong-policy"
                ? EventStoreDomainServicePolicies.Process : EventStoreDomainServicePolicies.Query));
            context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse(contextKind == "wrong-route" ? "/process" : "/query"), 0,
                new EndpointMetadataCollection(metadata), "matched-test-endpoint"));
        }
        var verifier = new TenantsGlobalAdministratorVerifier(authority, new HttpContextAccessor { HttpContext = context });
        (await verifier.IsCurrentGlobalAdministratorAsync(new("system", "tenants", "admin", "correlation"),
            TestContext.Current.CancellationToken)).ShouldBeFalse();
        if (stripped) await authority.DidNotReceiveWithAnyArgs().IsCurrentGlobalAdministratorAsync(default!, default);
        else await authority.Received(1).IsCurrentGlobalAdministratorAsync("admin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GlobalDomainHintsNeverTriggerASourceSelfCallOrGrantAuthority()
    {
        var authority = Substitute.For<ITenantsGlobalAdministratorAuthority>();
        var verifier = new TenantsGlobalAdministratorVerifier(authority);
        (await verifier.IsCurrentGlobalAdministratorAsync(new("system", "global-administrators", "current", "correlation"),
            TestContext.Current.CancellationToken)).ShouldBeFalse();
        await authority.DidNotReceiveWithAnyArgs().IsCurrentGlobalAdministratorAsync(default!, default);
    }
}
