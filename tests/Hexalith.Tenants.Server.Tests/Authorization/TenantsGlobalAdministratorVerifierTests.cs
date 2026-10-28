using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Authorization;
using NSubstitute;
using Shouldly;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

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
