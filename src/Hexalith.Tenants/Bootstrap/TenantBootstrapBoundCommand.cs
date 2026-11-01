using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Preserves a cryptographically authenticated administrator through the existing exact bound workload command protocol.</summary>
internal static class TenantBootstrapBoundCommand
{
    internal static async Task<HttpRequestMessage?> CreateAsync(IServiceProvider services, IConfiguration configuration,
        DaprClient dapr, SubmitCommandRequest command, string administrator, string accessToken, TimeProvider clock,
        TimeSpan readinessTimeout, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Resilience handlers legitimately disable the transport timeout. Catalog readiness
        // still gets HttpClient's normal finite budget rather than waiting indefinitely.
        if (readinessTimeout == Timeout.InfiniteTimeSpan) readinessTimeout = TimeSpan.FromSeconds(100);
        if (readinessTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(readinessTimeout));
        var context = new DefaultHttpContext { RequestServices = services, RequestAborted = token };
        context.Request.Headers.Authorization = "Bearer " + accessToken;
        // Use the owning host's real installed bearer handler, including OIDC signing-key
        // discovery, issuer, audience, signature and lifetime validation. Decoding sub is not authority.
        var authenticated = await services.GetRequiredService<IAuthenticationService>()
            .AuthenticateAsync(context, JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (!authenticated.Succeeded || authenticated.Principal is not { } principal) return null;
        var human = new ClaimsPrincipal(principal.Identities.Where(identity => identity.IsAuthenticated));
        Claim[] subjects = human.FindAll("sub").ToArray();
        if (subjects.Length != 1 || subjects[0].Value != administrator || !GlobalAdministratorHelper.IsGlobalAdministrator(human)
            || human.HasClaim(claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType)
            || command.Tenant != TenantIdentity.DefaultTenantId || command.Domain != TenantIdentity.GlobalAdministratorsDomain
            || command.AggregateId != TenantIdentity.GlobalAdministratorsAggregateId || command.CommandType != nameof(BootstrapGlobalAdmin)
            || command.Payload.Deserialize<BootstrapGlobalAdmin>(JsonSerializerOptions.Web)?.UserId != subjects[0].Value) return null;
        string? caller = configuration["Authentication:WorkloadIssuer:Workload"];
        string? privateKey = configuration["Authentication:BoundStreamRead:PrivateKey"];
        if (string.IsNullOrWhiteSpace(caller) || string.IsNullOrWhiteSpace(privateKey)) return null;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        if (services.GetService<IRouteCatalogAuthority>() is { } authority
            && configuration["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] is { Length: > 0 } root)
        {
            using var readinessDeadline = new CancellationTokenSource(readinessTimeout, clock);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token, readinessDeadline.Token);
            while (true)
            {
                try
                {
                    var readback = await authority.ReadbackAsync(deadline.Token).ConfigureAwait(false);
                    if (readback.Serving && readback.RootDigest == root) break;
                }
                catch (RouteCatalogUnavailableException) { }
                await Task.Delay(100, deadline.Token).ConfigureAwait(false);
            }
        }
#endif
        string? assertion = await services.GetRequiredService<IWorkloadAssertionIssuer>()
            .IssueAsync(new("eventstore", EventStoreWorkloadOperations.GatewayCommandSubmit), token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(assertion)) return null;
        var bound = new BoundWorkloadCommandRequest(command, subjects[0].Value);
        var request = dapr.CreateInvokeMethodRequest(HttpMethod.Post, "eventstore", "api/v1/commands/bound/workload");
        try
        {
            request.Content = JsonContent.Create(bound);
            // Dapr must see a known-length body so its timeout retry can replay the same
            // signed command instead of reusing a consumed chunked request stream.
            token.ThrowIfCancellationRequested();
            await request.Content.LoadIntoBufferAsync(token).ConfigureAwait(false);
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            request.Headers.Add(BoundWorkloadCommandProof.HeaderName, BoundWorkloadCommandProof.Issue(bound, caller, privateKey, clock.GetUtcNow()));
            return request;
        }
        catch
        {
            request.Dispose();
            throw;
        }
    }
}
